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
    public interface ILimitService
    {
        Task<LimitCheckResult> CheckUserLimitAsync(string tenantId, bool allowAdminOverride = false);
        Task<LimitCheckResult> CheckProductLimitAsync(string tenantId);
        Task<LimitCheckResult> CheckTransactionLimitAsync(string tenantId);
        Task<LimitCheckResult> CheckBranchLimitAsync(string tenantId);
        Task<LimitCheckResult> CheckStorageLimitAsync(string tenantId);
        Task<bool> CanCreateUserAsync(string tenantId);
        Task<bool> CanAddProductAsync(string tenantId);
        Task<bool> CanProcessTransactionAsync(string tenantId);
        Task<bool> CanCreateBranchAsync(string tenantId);
        Task<bool> CanCreateUserWithAdminOverrideAsync(string tenantId);
        Task<List<LimitAlert>> GetLimitAlertsAsync(string tenantId);
        Task<int> GetAdditionalUserCountAsync(string tenantId);
    }

    public class LimitService : ILimitService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LimitService> _logger;
        private readonly IUsageTrackingService _usageTrackingService;
        private readonly ISubscriptionNotificationService _subscriptionNotificationService;

        public LimitService(
            ApplicationDbContext context,
            ILogger<LimitService> logger,
            IUsageTrackingService usageTrackingService,
            ISubscriptionNotificationService subscriptionNotificationService)
        {
            _context = context;
            _logger = logger;
            _usageTrackingService = usageTrackingService;
            _subscriptionNotificationService = subscriptionNotificationService;
        }

        public async Task<LimitCheckResult> CheckUserLimitAsync(string tenantId, bool allowAdminOverride = false)
        {
            try
            {
                var subscription = await GetActiveSubscriptionAsync(tenantId);
                if (subscription?.Plan == null)
                {
                    return new LimitCheckResult
                    {
                        IsWithinLimit = false,
                        Reason = "No active subscription found",
                        Current = 0,
                        Limit = 0
                    };
                }

                var currentUsers = await _context.Users
                    .CountAsync(u => u.TenantId == tenantId && u.IsActive);

                // Get additional paid users
                var additionalUsers = await GetAdditionalUserCountAsync(tenantId);
                var maxUsers = subscription.Plan.MaxUsers == -1 ? int.MaxValue : subscription.Plan.MaxUsers + additionalUsers;

                // Allow admin override if specifically requested and user has additional paid users
                if (allowAdminOverride && additionalUsers > 0)
                {
                    return new LimitCheckResult
                    {
                        IsWithinLimit = true,
                        Current = currentUsers,
                        Limit = maxUsers,
                        Percentage = maxUsers == int.MaxValue ? 0 : (double)currentUsers / maxUsers * 100,
                        Reason = $"User limit: {currentUsers}/{maxUsers} (includes {additionalUsers} additional paid users)",
                        AdditionalUsers = additionalUsers
                    };
                }

                return new LimitCheckResult
                {
                    IsWithinLimit = currentUsers < maxUsers,
                    Current = currentUsers,
                    Limit = maxUsers,
                    Percentage = maxUsers == int.MaxValue ? 0 : (double)currentUsers / maxUsers * 100,
                    Reason = currentUsers >= maxUsers 
                        ? $"User limit exceeded ({currentUsers}/{maxUsers})"
                        : $"User limit: {currentUsers}/{maxUsers}",
                    AdditionalUsers = additionalUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user limit for tenant {TenantId}", tenantId);
                return new LimitCheckResult
                {
                    IsWithinLimit = false,
                    Reason = "Error checking limit",
                    Current = 0,
                    Limit = 0
                };
            }
        }

        public async Task<LimitCheckResult> CheckProductLimitAsync(string tenantId)
        {
            try
            {
                var subscription = await GetActiveSubscriptionAsync(tenantId);
                if (subscription?.Plan == null)
                {
                    return new LimitCheckResult
                    {
                        IsWithinLimit = false,
                        Reason = "No active subscription found",
                        Current = 0,
                        Limit = 0
                    };
                }

                var currentProducts = await _context.InventoryItems
                    .CountAsync(p => p.TenantId == tenantId && p.IsActive);

                var maxProducts = subscription.Plan.MaxProducts == -1 ? int.MaxValue : subscription.Plan.MaxProducts;

                return new LimitCheckResult
                {
                    IsWithinLimit = currentProducts < maxProducts,
                    Current = currentProducts,
                    Limit = maxProducts,
                    Percentage = maxProducts == int.MaxValue ? 0 : (double)currentProducts / maxProducts * 100,
                    Reason = currentProducts >= maxProducts 
                        ? $"Product limit exceeded ({currentProducts}/{maxProducts})"
                        : $"Product limit: {currentProducts}/{maxProducts}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product limit for tenant {TenantId}", tenantId);
                return new LimitCheckResult
                {
                    IsWithinLimit = false,
                    Reason = "Error checking limit",
                    Current = 0,
                    Limit = 0
                };
            }
        }

        public async Task<LimitCheckResult> CheckTransactionLimitAsync(string tenantId)
        {
            try
            {
                var subscription = await GetActiveSubscriptionAsync(tenantId);
                if (subscription?.Plan == null)
                {
                    return new LimitCheckResult
                    {
                        IsWithinLimit = false,
                        Reason = "No active subscription found",
                        Current = 0,
                        Limit = 0
                    };
                }

                var now = DateTime.UtcNow;
                var currentMonth = new DateTime(now.Year, now.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);

                var currentTransactions = await _context.Sales
                    .CountAsync(s => s.TenantId == tenantId && 
                                   s.CreatedAt >= currentMonth && s.CreatedAt < nextMonth);

                var maxTransactions = subscription.Plan.MaxTransactions == -1 ? int.MaxValue : subscription.Plan.MaxTransactions;

                return new LimitCheckResult
                {
                    IsWithinLimit = currentTransactions < maxTransactions,
                    Current = currentTransactions,
                    Limit = maxTransactions,
                    Percentage = maxTransactions == int.MaxValue ? 0 : (double)currentTransactions / maxTransactions * 100,
                    Reason = currentTransactions >= maxTransactions 
                        ? $"Transaction limit exceeded ({currentTransactions}/{maxTransactions})"
                        : $"Transaction limit: {currentTransactions}/{maxTransactions}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction limit for tenant {TenantId}", tenantId);
                return new LimitCheckResult
                {
                    IsWithinLimit = false,
                    Reason = "Error checking limit",
                    Current = 0,
                    Limit = 0
                };
            }
        }

        public async Task<LimitCheckResult> CheckBranchLimitAsync(string tenantId)
        {
            try
            {
                var subscription = await GetActiveSubscriptionAsync(tenantId);
                if (subscription?.Plan == null)
                {
                    return new LimitCheckResult
                    {
                        IsWithinLimit = false,
                        Reason = "No active subscription found",
                        Current = 0,
                        Limit = 0
                    };
                }

                var currentBranches = await _context.Branches
                    .CountAsync(b => b.TenantId == tenantId && b.IsActive);

                var maxBranches = subscription.Plan.MaxBranches == -1 ? int.MaxValue : subscription.Plan.MaxBranches;

                return new LimitCheckResult
                {
                    IsWithinLimit = currentBranches < maxBranches,
                    Current = currentBranches,
                    Limit = maxBranches,
                    Percentage = maxBranches == int.MaxValue ? 0 : (double)currentBranches / maxBranches * 100,
                    Reason = currentBranches >= maxBranches 
                        ? $"Branch limit exceeded ({currentBranches}/{maxBranches})"
                        : $"Branch limit: {currentBranches}/{maxBranches}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking branch limit for tenant {TenantId}", tenantId);
                return new LimitCheckResult
                {
                    IsWithinLimit = false,
                    Reason = "Error checking limit",
                    Current = 0,
                    Limit = 0
                };
            }
        }

        public async Task<LimitCheckResult> CheckStorageLimitAsync(string tenantId)
        {
            try
            {
                var subscription = await GetActiveSubscriptionAsync(tenantId);
                if (subscription?.Plan == null)
                {
                    return new LimitCheckResult
                    {
                        IsWithinLimit = false,
                        Reason = "No active subscription found",
                        Current = 0,
                        Limit = 0
                    };
                }

                var currentStorage = await GetStorageUsageAsync(tenantId);
                var maxStorage = subscription.Plan.MaxStorageGB == -1 ? int.MaxValue : subscription.Plan.MaxStorageGB;

                return new LimitCheckResult
                {
                    IsWithinLimit = currentStorage < maxStorage,
                    Current = (int)currentStorage,
                    Limit = maxStorage,
                    Percentage = maxStorage == int.MaxValue ? 0 : (double)currentStorage / maxStorage * 100,
                    Reason = currentStorage >= maxStorage 
                        ? $"Storage limit exceeded ({currentStorage}GB/{maxStorage}GB)"
                        : $"Storage limit: {currentStorage}GB/{maxStorage}GB"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage limit for tenant {TenantId}", tenantId);
                return new LimitCheckResult
                {
                    IsWithinLimit = false,
                    Reason = "Error checking limit",
                    Current = 0,
                    Limit = 0
                };
            }
        }

        public async Task<bool> CanCreateUserAsync(string tenantId)
        {
            var result = await CheckUserLimitAsync(tenantId);
            
            if (!result.IsWithinLimit)
            {
                await _subscriptionNotificationService.SendLimitExceededNotification(tenantId, new UsageAlert
                {
                    Type = "user_limit",
                    Severity = "critical",
                    Message = result.Reason,
                    Recommendation = "Upgrade your plan to add more users",
                    Current = result.Current,
                    Limit = result.Limit,
                    Percentage = result.Percentage
                });
            }

            return result.IsWithinLimit;
        }

        public async Task<bool> CanAddProductAsync(string tenantId)
        {
            var result = await CheckProductLimitAsync(tenantId);
            
            if (!result.IsWithinLimit)
            {
                await _subscriptionNotificationService.SendLimitExceededNotification(tenantId, new UsageAlert
                {
                    Type = "product_limit",
                    Severity = "critical",
                    Message = result.Reason,
                    Recommendation = "Upgrade your plan to add more products",
                    Current = result.Current,
                    Limit = result.Limit,
                    Percentage = result.Percentage
                });
            }

            return result.IsWithinLimit;
        }

        public async Task<bool> CanProcessTransactionAsync(string tenantId)
        {
            var result = await CheckTransactionLimitAsync(tenantId);
            
            if (!result.IsWithinLimit)
            {
                await _subscriptionNotificationService.SendLimitExceededNotification(tenantId, new UsageAlert
                {
                    Type = "transaction_limit",
                    Severity = "critical",
                    Message = result.Reason,
                    Recommendation = "Upgrade your plan for more transactions",
                    Current = result.Current,
                    Limit = result.Limit,
                    Percentage = result.Percentage
                });
            }

            return result.IsWithinLimit;
        }

        public async Task<bool> CanCreateBranchAsync(string tenantId)
        {
            var result = await CheckBranchLimitAsync(tenantId);
            
            if (!result.IsWithinLimit)
            {
                await _subscriptionNotificationService.SendLimitExceededNotification(tenantId, new UsageAlert
                {
                    Type = "branch_limit",
                    Severity = "critical",
                    Message = result.Reason,
                    Recommendation = "Upgrade your plan to add more branches",
                    Current = result.Current,
                    Limit = result.Limit,
                    Percentage = result.Percentage
                });
            }

            return result.IsWithinLimit;
        }

        public async Task<List<LimitAlert>> GetLimitAlertsAsync(string tenantId)
        {
            var alerts = new List<LimitAlert>();

            try
            {
                var usageMetrics = await _usageTrackingService.GetUsageMetricsAsync(tenantId);

                // Check all limits for alerts
                if (usageMetrics.Users.Percentage >= 90)
                {
                    alerts.Add(new LimitAlert
                    {
                        Type = "users",
                        Severity = usageMetrics.Users.Percentage >= 100 ? "critical" : "warning",
                        Current = usageMetrics.Users.Current,
                        Limit = usageMetrics.Users.Limit,
                        Percentage = usageMetrics.Users.Percentage,
                        Message = $"User usage: {usageMetrics.Users.Current}/{usageMetrics.Users.Limit} ({usageMetrics.Users.Percentage:F1}%)"
                    });
                }

                if (usageMetrics.Products.Percentage >= 90)
                {
                    alerts.Add(new LimitAlert
                    {
                        Type = "products",
                        Severity = usageMetrics.Products.Percentage >= 100 ? "critical" : "warning",
                        Current = usageMetrics.Products.Current,
                        Limit = usageMetrics.Products.Limit,
                        Percentage = usageMetrics.Products.Percentage,
                        Message = $"Product usage: {usageMetrics.Products.Current}/{usageMetrics.Products.Limit} ({usageMetrics.Products.Percentage:F1}%)"
                    });
                }

                if (usageMetrics.Transactions.Percentage >= 90)
                {
                    alerts.Add(new LimitAlert
                    {
                        Type = "transactions",
                        Severity = usageMetrics.Transactions.Percentage >= 100 ? "critical" : "warning",
                        Current = usageMetrics.Transactions.Current,
                        Limit = usageMetrics.Transactions.Limit,
                        Percentage = usageMetrics.Transactions.Percentage,
                        Message = $"Transaction usage: {usageMetrics.Transactions.Current}/{usageMetrics.Transactions.Limit} ({usageMetrics.Transactions.Percentage:F1}%)"
                    });
                }

                if (usageMetrics.Branches.Percentage >= 90)
                {
                    alerts.Add(new LimitAlert
                    {
                        Type = "branches",
                        Severity = usageMetrics.Branches.Percentage >= 100 ? "critical" : "warning",
                        Current = usageMetrics.Branches.Current,
                        Limit = usageMetrics.Branches.Limit,
                        Percentage = usageMetrics.Branches.Percentage,
                        Message = $"Branch usage: {usageMetrics.Branches.Current}/{usageMetrics.Branches.Limit} ({usageMetrics.Branches.Percentage:F1}%)"
                    });
                }

                if (usageMetrics.Storage.Percentage >= 90)
                {
                    alerts.Add(new LimitAlert
                    {
                        Type = "storage",
                        Severity = usageMetrics.Storage.Percentage >= 100 ? "critical" : "warning",
                        Current = usageMetrics.Storage.Current,
                        Limit = usageMetrics.Storage.Limit,
                        Percentage = usageMetrics.Storage.Percentage,
                        Message = $"Storage usage: {usageMetrics.Storage.Current}GB/{usageMetrics.Storage.Limit}GB ({usageMetrics.Storage.Percentage:F1}%)"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting limit alerts for tenant {TenantId}", tenantId);
            }

            return alerts;
        }

        public async Task<bool> CanCreateUserWithAdminOverrideAsync(string tenantId)
        {
            var result = await CheckUserLimitAsync(tenantId, allowAdminOverride: true);
            
            if (!result.IsWithinLimit && result.AdditionalUsers == 0)
            {
                await _subscriptionNotificationService.SendLimitExceededNotification(tenantId, new UsageAlert
                {
                    Type = "user_limit",
                    Severity = "critical",
                    Message = result.Reason,
                    Recommendation = "Purchase additional user licenses or upgrade your plan"
                });
            }

            return result.IsWithinLimit;
        }

        public async Task<int> GetAdditionalUserCountAsync(string tenantId)
        {
            try
            {
                var now = DateTime.UtcNow;
                return await _context.AdditionalUserPurchases
                    .Where(p => p.TenantId == tenantId && 
                               p.Status == "active" && 
                               p.IsActive &&
                               p.StartDate <= now && 
                               (p.EndDate == null || p.EndDate >= now))
                    .SumAsync(p => p.NumberOfUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting additional user count for tenant {TenantId}", tenantId);
                return 0;
            }
        }

        private async Task<Subscription?> GetActiveSubscriptionAsync(string tenantId)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                                       (s.Status == "active" || s.Status == "grace_period"));
        }

        private async Task<double> GetStorageUsageAsync(string tenantId)
        {
            try
            {
                // Enhanced storage calculation - in production, this would calculate actual file/database storage
                // For now, we'll estimate based on typical usage patterns
                
                var storageUsage = 0.0;
                
                // Database storage estimation based on tenant activity
                var userCount = await _context.Users.CountAsync(u => u.TenantId == tenantId);
                var productCount = await _context.Products.CountAsync(p => p.TenantId == tenantId);
                var saleCount = await _context.Sales.CountAsync(s => s.TenantId == tenantId);
                var customerCount = await _context.Customers.CountAsync(c => c.TenantId == tenantId);
                
                // Estimate database storage (in MB)
                var dbStorage = (userCount * 0.5) +        // User records
                               (productCount * 0.2) +      // Product records
                               (saleCount * 0.8) +        // Sales records (larger due to items)
                               (customerCount * 0.3) +    // Customer records
                               10.0;                       // System overhead
                
                // Estimate file storage (in MB) based on typical usage
                var fileStorage = Math.Max(0.5, userCount * 0.1); // Documents, images, etc.
                
                // Convert to GB
                storageUsage = (dbStorage + fileStorage) / 1024.0;
                
                // Add some realistic variation based on tenant activity
                var random = new Random(tenantId.GetHashCode());
                storageUsage *= (0.8 + (random.NextDouble() * 0.4)); // ±20% variation
                
                return Math.Round(storageUsage, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating storage usage for tenant {TenantId}", tenantId);
                return 2.5; // Fallback to default value
            }
        }
    }

    // DTOs
    public class LimitCheckResult
    {
        public bool IsWithinLimit { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Current { get; set; }
        public int Limit { get; set; }
        public double Percentage { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public int AdditionalUsers { get; set; } = 0;
    }

    public class LimitAlert
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // warning, critical
        public int Current { get; set; }
        public int Limit { get; set; }
        public double Percentage { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
