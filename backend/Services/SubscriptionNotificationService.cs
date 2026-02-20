using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.DTOs;

namespace UmiHealthPOS.Services
{
    public interface ISubscriptionNotificationService
    {
        Task SendSubscriptionExpiredNotification(Subscription subscription);
        Task SendGracePeriodNotification(Subscription subscription);
        Task SendExpirationWarning(Subscription subscription, int daysUntilExpiration);
        Task SendLimitExceededNotification(string tenantId, UsageAlert alert);
        Task SendLimitApproachingNotification(string tenantId, UsageAlert alert);
        Task SendUpgradePrompt(string tenantId, string feature, string requiredPlan);
    }

    public class SubscriptionNotificationService : ISubscriptionNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionNotificationService> _logger;
        private readonly INotificationService _notificationService;

        public SubscriptionNotificationService(
            ApplicationDbContext context,
            ILogger<SubscriptionNotificationService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task SendSubscriptionExpiredNotification(Subscription subscription)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(subscription.TenantId);
                var adminUsers = await GetTenantAdminUsersAsync(subscription.TenantId);

                var title = "Subscription Expired";
                var message = $"Your subscription has expired. Please renew to continue using Umi Health POS services.";

                foreach (var user in adminUsers)
                {
                    await CreateNotification(user.UserId, title, message, "subscription", "critical", new
                    {
                        subscriptionId = subscription.Id,
                        planName = subscription.Plan?.Name,
                        expiredDate = subscription.EndDate,
                        actionUrl = "/billing/renew"
                    });
                }

                _logger.LogInformation(
                    "Subscription expired notification sent for tenant {TenantId}, subscription {SubscriptionId}",
                    subscription.TenantId, subscription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending subscription expired notification");
            }
        }

        public async Task SendGracePeriodNotification(Subscription subscription)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(subscription.TenantId);
                var adminUsers = await GetTenantAdminUsersAsync(subscription.TenantId);

                var title = "Grace Period Started";
                var message = $"Your subscription has entered a 7-day grace period. Please renew to avoid service interruption.";

                foreach (var user in adminUsers)
                {
                    await CreateNotification(user.UserId, title, message, "subscription", "warning", new
                    {
                        subscriptionId = subscription.Id,
                        planName = subscription.Plan?.Name,
                        gracePeriodEnd = subscription.EndDate.AddDays(7),
                        actionUrl = "/billing/renew"
                    });
                }

                _logger.LogInformation(
                    "Grace period notification sent for tenant {TenantId}, subscription {SubscriptionId}",
                    subscription.TenantId, subscription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending grace period notification");
            }
        }

        public async Task SendExpirationWarning(Subscription subscription, int daysUntilExpiration)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(subscription.TenantId);
                var adminUsers = await GetTenantAdminUsersAsync(subscription.TenantId);

                var severity = daysUntilExpiration <= 3 ? "critical" : "warning";
                var title = daysUntilExpiration == 1 ? "Subscription Expires Tomorrow" : $"Subscription Expires in {daysUntilExpiration} Days";
                var message = $"Your subscription will expire in {daysUntilExpiration} day(s). Please renew to continue using our services.";

                foreach (var user in adminUsers)
                {
                    await CreateNotification(user.UserId, title, message, "subscription", severity, new
                    {
                        subscriptionId = subscription.Id,
                        planName = subscription.Plan?.Name,
                        expirationDate = subscription.EndDate,
                        daysUntilExpiration = daysUntilExpiration,
                        actionUrl = "/billing/renew"
                    });
                }

                _logger.LogInformation(
                    "Expiration warning sent for tenant {TenantId}, subscription {SubscriptionId}, {Days} days remaining",
                    subscription.TenantId, subscription.Id, daysUntilExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiration warning");
            }
        }

        public async Task SendLimitExceededNotification(string tenantId, UsageAlert alert)
        {
            try
            {
                var adminUsers = await GetTenantAdminUsersAsync(tenantId);

                var title = $"{alert.Type.ToUpperInvariant()} Limit Exceeded";
                var message = $"You have exceeded your {alert.Type} limit. {alert.Message}";

                foreach (var user in adminUsers)
                {
                    await CreateNotification(user.UserId, title, message, "limit", "critical", new
                    {
                        alertType = alert.Type,
                        currentUsage = alert.Current,
                        limit = alert.Limit,
                        percentage = alert.Percentage,
                        recommendation = alert.Recommendation,
                        actionUrl = "/billing/upgrade"
                    });
                }

                _logger.LogWarning(
                    "Limit exceeded notification sent for tenant {TenantId}, type {AlertType}",
                    tenantId, alert.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending limit exceeded notification");
            }
        }

        public async Task SendLimitApproachingNotification(string tenantId, UsageAlert alert)
        {
            try
            {
                var adminUsers = await GetTenantAdminUsersAsync(tenantId);

                var title = $"{alert.Type.ToUpperInvariant()} Limit Warning";
                var message = $"You are approaching your {alert.Type} limit. {alert.Message}";

                foreach (var user in adminUsers)
                {
                    await CreateNotification(user.UserId, title, message, "limit", "warning", new
                    {
                        alertType = alert.Type,
                        currentUsage = alert.Current,
                        limit = alert.Limit,
                        percentage = alert.Percentage,
                        recommendation = alert.Recommendation,
                        actionUrl = "/billing/upgrade"
                    });
                }

                _logger.LogInformation(
                    "Limit approaching notification sent for tenant {TenantId}, type {AlertType}",
                    tenantId, alert.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending limit approaching notification");
            }
        }

        public async Task SendUpgradePrompt(string tenantId, string feature, string requiredPlan)
        {
            try
            {
                var adminUsers = await GetTenantAdminUsersAsync(tenantId);

                var title = "Feature Upgrade Required";
                var message = $"The '{feature}' feature requires a {requiredPlan} subscription or higher.";

                foreach (var user in adminUsers)
                {
                    await CreateNotification(user.UserId, title, message, "upgrade", "info", new
                    {
                        feature = feature,
                        requiredPlan = requiredPlan,
                        actionUrl = "/billing/upgrade"
                    });
                }

                _logger.LogInformation(
                    "Upgrade prompt sent for tenant {TenantId}, feature {Feature}, required plan {RequiredPlan}",
                    tenantId, feature, requiredPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending upgrade prompt");
            }
        }

        private async Task<List<UserAccount>> GetTenantAdminUsersAsync(string tenantId)
        {
            return await _context.Users
                .Where(u => u.TenantId == tenantId &&
                           u.IsActive &&
                           (u.Role == "admin" || u.Role == "TenantAdmin"))
                .ToListAsync();
        }

        private async Task CreateNotification(
            string userId,
            string title,
            string message,
            string category,
            string severity,
            object metadata)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Category = category,
                Type = severity,
                IsRead = false,
                Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}
