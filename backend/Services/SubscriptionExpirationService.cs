using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface ISubscriptionExpirationService
    {
        Task CheckAndUpdateExpiredSubscriptions();
        Task<List<Subscription>> GetExpiringSoonSubscriptions(int daysAhead = 7);
        Task<List<Subscription>> GetExpiredSubscriptions();
        Task UpdateSubscriptionStatus(int subscriptionId, string status);
    }

    public class SubscriptionExpirationService : BackgroundService, ISubscriptionExpirationService
    {
        private readonly ILogger<SubscriptionExpirationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SubscriptionExpirationService(
            ILogger<SubscriptionExpirationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiration Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateExpiredSubscriptions();
                    await SendExpirationWarnings();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking subscription expirations.");
                }

                // Check every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Subscription Expiration Service is stopping.");
        }

        public async Task CheckAndUpdateExpiredSubscriptions()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISubscriptionNotificationService>();

            try
            {
                var now = DateTime.UtcNow;
                var gracePeriodEnd = now.AddDays(-7); // 7-day grace period

                // Find subscriptions that are past their end date (including grace period)
                var expiredSubscriptions = await context.Subscriptions
                    .Include(s => s.Tenant)
                    .Include(s => s.Plan)
                    .Where(s => s.Status == "active" && s.EndDate < gracePeriodEnd)
                    .ToListAsync();

                foreach (var subscription in expiredSubscriptions)
                {
                    subscription.Status = "expired";
                    subscription.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Subscription {SubscriptionId} for tenant {TenantName} has been marked as expired",
                        subscription.Id, subscription.Tenant?.Name);

                    // Send expiration notification
                    await notificationService.SendSubscriptionExpiredNotification(subscription);
                }

                // Find subscriptions that just entered grace period
                var gracePeriodSubscriptions = await context.Subscriptions
                    .Include(s => s.Tenant)
                    .Include(s => s.Plan)
                    .Where(s => s.Status == "active" && 
                               s.EndDate >= gracePeriodEnd && 
                               s.EndDate < now)
                    .ToListAsync();

                foreach (var subscription in gracePeriodSubscriptions)
                {
                    subscription.Status = "grace_period";
                    subscription.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Subscription {SubscriptionId} for tenant {TenantName} has entered grace period",
                        subscription.Id, subscription.Tenant?.Name);

                    // Send grace period notification
                    await notificationService.SendGracePeriodNotification(subscription);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expired subscriptions");
                throw;
            }
        }

        public async Task<List<Subscription>> GetExpiringSoonSubscriptions(int daysAhead = 7)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
            
            return await context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.Plan)
                .Where(s => s.Status == "active" && 
                           s.EndDate <= cutoffDate && 
                           s.EndDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetExpiredSubscriptions()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.Plan)
                .Where(s => s.Status == "expired" || s.Status == "grace_period")
                .ToListAsync();
        }

        public async Task UpdateSubscriptionStatus(int subscriptionId, string status)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var subscription = await context.Subscriptions.FindAsync(subscriptionId);
            if (subscription != null)
            {
                subscription.Status = status;
                subscription.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription {SubscriptionId} status updated to {Status}",
                    subscriptionId, status);
            }
        }

        private async Task SendExpirationWarnings()
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISubscriptionNotificationService>();
            try
            {
                // Send 7-day warnings
                var sevenDayWarnings = await GetExpiringSoonSubscriptions(7);
                foreach (var subscription in sevenDayWarnings)
                {
                    await notificationService.SendExpirationWarning(subscription, 7);
                }

                // Send 3-day warnings
                var threeDayWarnings = await GetExpiringSoonSubscriptions(3);
                foreach (var subscription in threeDayWarnings)
                {
                    await notificationService.SendExpirationWarning(subscription, 3);
                }

                // Send 1-day warnings
                var oneDayWarnings = await GetExpiringSoonSubscriptions(1);
                foreach (var subscription in oneDayWarnings)
                {
                    await notificationService.SendExpirationWarning(subscription, 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiration warnings");
            }
        }
    }

    public class SubscriptionExpirationResult
    {
        public int ExpiredCount { get; set; }
        public int GracePeriodCount { get; set; }
        public int WarningSentCount { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }
}
