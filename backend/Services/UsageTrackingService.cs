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
    public interface IUsageTrackingService
    {
        Task<UsageMetrics> GetUsageMetricsAsync(string tenantId);
        Task<UsageHistory> GetUsageHistoryAsync(string tenantId, DateTime startDate, DateTime endDate);
        Task RecordUserActivityAsync(string tenantId, string userId, string activity);
        Task RecordTransactionAsync(string tenantId, decimal amount);
        Task RecordProductOperationAsync(string tenantId, string operation);
        Task<List<UsageAlert>> GetUsageAlertsAsync(string tenantId);
        Task<UsageAnalytics> GetUsageAnalyticsAsync(string tenantId);
        Task<bool> IsApproachingLimitAsync(string tenantId, string metricType, double threshold = 0.9);
    }

    public class UsageTrackingService : IUsageTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsageTrackingService> _logger;
        private readonly ISubscriptionNotificationService _subscriptionNotificationService;

        public UsageTrackingService(
            ApplicationDbContext context,
            ILogger<UsageTrackingService> logger,
            ISubscriptionNotificationService subscriptionNotificationService)
        {
            _context = context;
            _logger = logger;
            _subscriptionNotificationService = subscriptionNotificationService;
        }

        public async Task<UsageMetrics> GetUsageMetricsAsync(string tenantId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var currentMonth = new DateTime(now.Year, now.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);

                // Get subscription plan
                var subscription = await _context.Subscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "active");

                if (subscription?.Plan == null)
                {
                    return new UsageMetrics { TenantId = tenantId };
                }

                // Count current usage
                var userCount = await _context.Users
                    .CountAsync(u => u.TenantId == tenantId && u.IsActive);

                var productCount = await _context.InventoryItems
                    .CountAsync(p => p.TenantId == tenantId && p.IsActive);

                var transactionCount = await _context.Sales
                    .CountAsync(s => s.TenantId == tenantId &&
                                   s.CreatedAt >= currentMonth && s.CreatedAt < nextMonth);

                var branchCount = await _context.Branches
                    .CountAsync(b => b.TenantId == tenantId && b.IsActive);

                // Calculate usage percentages
                var plan = subscription.Plan;

                return new UsageMetrics
                {
                    TenantId = tenantId,
                    Users = new UsageMetric
                    {
                        Current = userCount,
                        Limit = plan.MaxUsers == -1 ? int.MaxValue : plan.MaxUsers,
                        Percentage = plan.MaxUsers == -1 ? 0 : (double)userCount / plan.MaxUsers * 100
                    },
                    Products = new UsageMetric
                    {
                        Current = productCount,
                        Limit = plan.MaxProducts == -1 ? int.MaxValue : plan.MaxProducts,
                        Percentage = plan.MaxProducts == -1 ? 0 : (double)productCount / plan.MaxProducts * 100
                    },
                    Transactions = new UsageMetric
                    {
                        Current = transactionCount,
                        Limit = plan.MaxTransactions == -1 ? int.MaxValue : plan.MaxTransactions,
                        Percentage = plan.MaxTransactions == -1 ? 0 : (double)transactionCount / plan.MaxTransactions * 100
                    },
                    Branches = new UsageMetric
                    {
                        Current = branchCount,
                        Limit = plan.MaxBranches == -1 ? int.MaxValue : plan.MaxBranches,
                        Percentage = plan.MaxBranches == -1 ? 0 : (double)branchCount / plan.MaxBranches * 100
                    },
                    Storage = new UsageMetric
                    {
                        Current = (int)Math.Round(await GetStorageUsageAsync(tenantId)),
                        Limit = plan.MaxStorageGB == -1 ? int.MaxValue : plan.MaxStorageGB,
                        Percentage = plan.MaxStorageGB == -1 ? 0 : (int)Math.Round(await GetStorageUsagePercentageAsync(tenantId, plan.MaxStorageGB))
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage metrics for tenant {TenantId}", tenantId);
                return new UsageMetrics { TenantId = tenantId };
            }
        }

        public async Task<UsageHistory> GetUsageHistoryAsync(string tenantId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var dailyUsage = new List<DailyUsage>();

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var nextDate = date.AddDays(1);

                    var dailyTransactions = await _context.Sales
                        .CountAsync(s => s.TenantId == tenantId &&
                                       s.CreatedAt >= date && s.CreatedAt < nextDate);

                    var dailyRevenue = await _context.Sales
                        .Where(s => s.TenantId == tenantId &&
                                   s.CreatedAt >= date && s.CreatedAt < nextDate)
                        .SumAsync(s => s.Total);

                    dailyUsage.Add(new DailyUsage
                    {
                        Date = date,
                        Transactions = dailyTransactions,
                        Revenue = dailyRevenue
                    });
                }

                return new UsageHistory
                {
                    TenantId = tenantId,
                    StartDate = startDate,
                    EndDate = endDate,
                    DailyUsage = dailyUsage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage history for tenant {TenantId}", tenantId);
                return new UsageHistory { TenantId = tenantId };
            }
        }

        public async Task RecordUserActivityAsync(string tenantId, string userId, string activity)
        {
            try
            {
                var usageRecord = new UsageRecord
                {
                    TenantId = tenantId,
                    UserId = userId,
                    ActivityType = activity,
                    Timestamp = DateTime.UtcNow,
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        action = activity,
                        timestamp = DateTime.UtcNow
                    })
                };

                _context.UsageRecords.Add(usageRecord);
                await _context.SaveChangesAsync();

                // Check if user is approaching limits
                await CheckAndNotifyApproachingLimits(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording user activity for tenant {TenantId}", tenantId);
            }
        }

        public async Task RecordTransactionAsync(string tenantId, decimal amount)
        {
            try
            {
                var transactionRecord = new UsageRecord
                {
                    TenantId = tenantId,
                    ActivityType = "transaction",
                    Timestamp = DateTime.UtcNow,
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        amount = amount,
                        type = "sale"
                    })
                };

                _context.UsageRecords.Add(transactionRecord);
                await _context.SaveChangesAsync();

                // Check transaction limits
                await CheckAndNotifyApproachingLimits(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording transaction for tenant {TenantId}", tenantId);
            }
        }

        public async Task RecordProductOperationAsync(string tenantId, string operation)
        {
            try
            {
                var productRecord = new UsageRecord
                {
                    TenantId = tenantId,
                    ActivityType = "product_operation",
                    Timestamp = DateTime.UtcNow,
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        operation = operation,
                        timestamp = DateTime.UtcNow
                    })
                };

                _context.UsageRecords.Add(productRecord);
                await _context.SaveChangesAsync();

                // Check product limits
                await CheckAndNotifyApproachingLimits(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording product operation for tenant {TenantId}", tenantId);
            }
        }

        public async Task<List<UsageAlert>> GetUsageAlertsAsync(string tenantId)
        {
            try
            {
                var metrics = await GetUsageMetricsAsync(tenantId);
                var alerts = new List<UsageAlert>();

                // Check each metric for alerts
                if (metrics.Users.Percentage >= 90)
                {
                    alerts.Add(new UsageAlert
                    {
                        Type = "user_limit",
                        Severity = metrics.Users.Percentage >= 100 ? "critical" : "warning",
                        Message = $"User limit: {metrics.Users.Current}/{metrics.Users.Limit} ({metrics.Users.Percentage:F1}%)",
                        Recommendation = metrics.Users.Percentage >= 100 ? "Upgrade your plan immediately" : "Consider upgrading soon",
                        Current = metrics.Users.Current,
                        Limit = metrics.Users.Limit,
                        Percentage = metrics.Users.Percentage
                    });
                }

                if (metrics.Products.Percentage >= 90)
                {
                    alerts.Add(new UsageAlert
                    {
                        Type = "product_limit",
                        Severity = metrics.Products.Percentage >= 100 ? "critical" : "warning",
                        Message = $"Product limit: {metrics.Products.Current}/{metrics.Products.Limit} ({metrics.Products.Percentage:F1}%)",
                        Recommendation = metrics.Products.Percentage >= 100 ? "Upgrade your plan immediately" : "Consider upgrading soon",
                        Current = metrics.Products.Current,
                        Limit = metrics.Products.Limit,
                        Percentage = metrics.Products.Percentage
                    });
                }

                if (metrics.Transactions.Percentage >= 90)
                {
                    alerts.Add(new UsageAlert
                    {
                        Type = "transaction_limit",
                        Severity = metrics.Transactions.Percentage >= 100 ? "critical" : "warning",
                        Message = $"Transaction limit: {metrics.Transactions.Current}/{metrics.Transactions.Limit} ({metrics.Transactions.Percentage:F1}%)",
                        Recommendation = metrics.Transactions.Percentage >= 100 ? "Upgrade your plan immediately" : "Consider upgrading soon",
                        Current = metrics.Transactions.Current,
                        Limit = metrics.Transactions.Limit,
                        Percentage = metrics.Transactions.Percentage
                    });
                }

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage alerts for tenant {TenantId}", tenantId);
                return new List<UsageAlert>();
            }
        }

        public async Task<UsageAnalytics> GetUsageAnalyticsAsync(string tenantId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var lastMonth = now.AddMonths(-1);
                var lastYear = now.AddYears(-1);

                // Get current metrics
                var currentMetrics = await GetUsageMetricsAsync(tenantId);

                // Get historical data for trends
                var lastMonthMetrics = await GetUsageMetricsAsync(tenantId);
                var lastYearMetrics = await GetUsageMetricsAsync(tenantId);

                // Calculate growth rates
                var userGrowth = CalculateGrowthRate(currentMetrics.Users.Current, lastMonthMetrics.Users.Current);
                var productGrowth = CalculateGrowthRate(currentMetrics.Products.Current, lastMonthMetrics.Products.Current);
                var transactionGrowth = CalculateGrowthRate(currentMetrics.Transactions.Current, lastMonthMetrics.Transactions.Current);

                return new UsageAnalytics
                {
                    TenantId = tenantId,
                    CurrentMetrics = currentMetrics,
                    UserGrowthRate = userGrowth,
                    ProductGrowthRate = productGrowth,
                    TransactionGrowthRate = transactionGrowth,
                    PeakUsageHours = await GetPeakUsageHoursAsync(tenantId),
                    MostUsedFeatures = await GetMostUsedFeaturesAsync(tenantId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage analytics for tenant {TenantId}", tenantId);
                return new UsageAnalytics { TenantId = tenantId };
            }
        }

        public async Task<bool> IsApproachingLimitAsync(string tenantId, string metricType, double threshold = 0.9)
        {
            try
            {
                var metrics = await GetUsageMetricsAsync(tenantId);

                return metricType.ToLower() switch
                {
                    "users" => metrics.Users.Percentage >= threshold * 100,
                    "products" => metrics.Products.Percentage >= threshold * 100,
                    "transactions" => metrics.Transactions.Percentage >= threshold * 100,
                    "branches" => metrics.Branches.Percentage >= threshold * 100,
                    "storage" => metrics.Storage.Percentage >= threshold * 100,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking limit approach for tenant {TenantId}", tenantId);
                return false;
            }
        }

        private async Task<double> GetStorageUsageAsync(string tenantId)
        {
            // This is a simplified implementation
            // In a real scenario, you would calculate actual file/database storage
            return await Task.FromResult(2.5); // GB
        }

        private async Task<double> GetStorageUsagePercentageAsync(string tenantId, int maxStorageGB)
        {
            if (maxStorageGB == -1) return 0;

            var currentUsage = await GetStorageUsageAsync(tenantId);
            return (currentUsage / maxStorageGB) * 100;
        }

        private async Task CheckAndNotifyApproachingLimits(string tenantId)
        {
            try
            {
                var alerts = await GetUsageAlertsAsync(tenantId);
                foreach (var alert in alerts)
                {
                    if (alert.Severity == "critical")
                    {
                        await _subscriptionNotificationService.SendLimitExceededNotification(tenantId, alert);
                    }
                    else if (alert.Severity == "warning")
                    {
                        await _subscriptionNotificationService.SendLimitApproachingNotification(tenantId, alert);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and notifying approaching limits for tenant {TenantId}", tenantId);
            }
        }

        private double CalculateGrowthRate(int current, int previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return ((double)(current - previous) / previous) * 100;
        }

        private async Task<List<int>> GetPeakUsageHoursAsync(string tenantId)
        {
            // Analyze usage records to find peak hours
            var usageByHour = await _context.UsageRecords
                .Where(r => r.TenantId == tenantId && r.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(r => r.Timestamp.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(3)
                .ToListAsync();

            return usageByHour.Select(u => u.Hour).ToList();
        }

        private async Task<List<string>> GetMostUsedFeaturesAsync(string tenantId)
        {
            // Analyze usage records to find most used features
            var features = await _context.UsageRecords
                .Where(r => r.TenantId == tenantId && r.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(r => r.ActivityType)
                .Select(g => new { Feature = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();

            return features.Select(f => f.Feature).ToList();
        }
    }

    // DTOs and Models
    public class UsageMetrics
    {
        public string TenantId { get; set; } = string.Empty;
        public UsageMetric Users { get; set; } = new();
        public UsageMetric Products { get; set; } = new();
        public UsageMetric Transactions { get; set; } = new();
        public UsageMetric Branches { get; set; } = new();
        public UsageMetric Storage { get; set; } = new();
    }

    public class UsageMetric
    {
        public int Current { get; set; }
        public int Limit { get; set; }
        public double Percentage { get; set; }
    }

    public class UsageHistory
    {
        public string TenantId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailyUsage> DailyUsage { get; set; } = new();
    }

    public class DailyUsage
    {
        public DateTime Date { get; set; }
        public int Transactions { get; set; }
        public decimal Revenue { get; set; }
    }

    public class UsageAnalytics
    {
        public string TenantId { get; set; } = string.Empty;
        public UsageMetrics CurrentMetrics { get; set; } = new();
        public double UserGrowthRate { get; set; }
        public double ProductGrowthRate { get; set; }
        public double TransactionGrowthRate { get; set; }
        public List<int> PeakUsageHours { get; set; } = new();
        public List<string> MostUsedFeatures { get; set; } = new();
    }
}
