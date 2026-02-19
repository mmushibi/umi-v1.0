using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealthPOS.Hubs;
using UmiHealthPOS.Models.Dashboard;

namespace UmiHealthPOS.Services
{
    public interface IDashboardNotificationService
    {
        Task NotifyStatsUpdate(string tenantId, DashboardStats stats);
        Task NotifyActivityUpdate(string tenantId, RecentActivity activity);
        Task NotifyLowStockAlert(string tenantId, string productName, int currentStock, int reorderLevel);
        Task NotifyNewSale(string tenantId, string saleId, decimal amount);
        Task NotifyNewPrescription(string tenantId, string prescriptionId, string patientName);
    }

    public class DashboardNotificationService : IDashboardNotificationService
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<DashboardNotificationService> _logger;

        public DashboardNotificationService(
            IHubContext<DashboardHub> hubContext,
            ILogger<DashboardNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyStatsUpdate(string tenantId, DashboardStats stats)
        {
            try
            {
                await _hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("DashboardStatsUpdate", new
                    {
                        type = "stats",
                        payload = stats,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation($"Stats update sent to tenant {tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending stats update to tenant {tenantId}");
            }
        }

        public async Task NotifyActivityUpdate(string tenantId, RecentActivity activity)
        {
            try
            {
                await _hubContext.Clients.Group($"tenant_{tenantId}")
                    .SendAsync("DashboardActivityUpdate", new
                    {
                        type = "activity",
                        payloMessage = activity.Description,
                        Timestamp = activity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                _logger.LogInformation($"Activity update sent to tenant {tenantId}: {activity.Description}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending activity update to tenant {tenantId}");
            }
        }

        public async Task NotifyLowStockAlert(string tenantId, string productName, int currentStock, int reorderLevel)
        {
            try
            {
                var activity = new RecentActivity
                {
                    Id = DateTime.UtcNow.GetHashCode(),
                    Type = "inventory",
                    Description = $"Low stock alert: {productName} ({currentStock} remaining, reorder at {reorderLevel})",
                    Timestamp = DateTime.UtcNow
                };

                await NotifyActivityUpdate(tenantId, activity);

                _logger.LogInformation($"Low stock alert sent to tenant {tenantId} for product {productName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending low stock alert to tenant {tenantId}");
            }
        }

        public async Task NotifyNewSale(string tenantId, string saleId, decimal amount)
        {
            try
            {
                var activity = new RecentActivity
                {
                    Id = DateTime.UtcNow.GetHashCode(),
                    Type = "sale",
                    Description = $"New sale completed: {amount:C} (ID: {saleId})",
                    Timestamp = DateTime.UtcNow
                };

                await NotifyActivityUpdate(tenantId, activity);

                _logger.LogInformation($"New sale notification sent to tenant {tenantId}: {saleId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending new sale notification to tenant {tenantId}");
            }
        }

        public async Task NotifyNewPrescription(string tenantId, string prescriptionId, string patientName)
        {
            try
            {
                var activity = new RecentActivity
                {
                    Id = DateTime.UtcNow.GetHashCode(),
                    Type = "prescription",
                    Description = $"New prescription for {patientName} (ID: {prescriptionId})",
                    Timestamp = DateTime.UtcNow
                };

                await NotifyActivityUpdate(tenantId, activity);

                _logger.LogInformation($"New prescription notification sent to tenant {tenantId}: {prescriptionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending new prescription notification to tenant {tenantId}");
            }
        }
    }
}
