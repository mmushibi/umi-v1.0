using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace UmiHealthPOS.Services
{
    public interface ISubscriptionPlanService
    {
        Task<List<SubscriptionPlan>> GetAvailablePlansAsync();
        Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
        Task<SubscriptionPlan?> GetPlanByPlanIdAsync(string planIdentifier);
        decimal CalculatePrice(int planId, string billingCycle);
        int GetDurationMonths(string billingCycle);
        bool ValidatePlanChange(int currentPlanId, int newPlanId);
    }

    // Helper classes for plan change validation
    public class PlanChangeValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PaymentStatus
    {
        public bool IsCurrent { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionPlanService> _logger;

        // Predefined plan configurations for Zambia market
        private static readonly Dictionary<int, (string Identifier, string Name, decimal Monthly, decimal Quarterly, decimal Yearly)> PlanConfigurations = new()
        {
            { 1, ("basic", "Basic", 299.00m, 849.00m, 3192.00m) },
            { 2, ("professional", "Professional", 599.00m, 1709.00m, 6432.00m) },
            { 3, ("enterprise", "Enterprise", 999.00m, 2849.00m, 10788.00m) }
        };

        public SubscriptionPlanService(ApplicationDbContext context, ILogger<SubscriptionPlanService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SubscriptionPlan>> GetAvailablePlansAsync()
        {
            try
            {
                return await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Price)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plans");
                return new List<SubscriptionPlan>();
            }
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
        {
            try
            {
                return await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plan {PlanId}", planId);
                return null;
            }
        }

        public async Task<SubscriptionPlan?> GetPlanByPlanIdAsync(string planIdentifier)
        {
            try
            {
                // Try to parse as integer first (for backward compatibility)
                if (int.TryParse(planIdentifier, out int planId))
                {
                    return await GetPlanByIdAsync(planId);
                }

                // Try to find by name or identifier
                var plan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => 
                        (p.Name.Equals(planIdentifier, StringComparison.OrdinalIgnoreCase) ||
                         p.Name.ToLower().Contains(planIdentifier.ToLower())) &&
                        p.IsActive);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plan by identifier {PlanIdentifier}", planIdentifier);
                return null;
            }
        }

        public decimal CalculatePrice(int planId, string billingCycle)
        {
            try
            {
                if (PlanConfigurations.TryGetValue(planId, out var config))
                {
                    return billingCycle.ToLower() switch
                    {
                        "monthly" => config.Monthly,
                        "quarterly" => config.Quarterly,
                        "yearly" => config.Yearly,
                        _ => config.Monthly
                    };
                }

                // Fallback to database pricing
                var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.Id == planId);
                if (plan != null)
                {
                    return billingCycle.ToLower() switch
                    {
                        "monthly" => plan.MonthlyPrice,
                        "quarterly" => plan.MonthlyPrice * 3 * 0.95m, // 5% discount for quarterly
                        "yearly" => plan.YearlyPrice > 0 ? plan.YearlyPrice : plan.MonthlyPrice * 12 * 0.90m, // 10% discount for yearly
                        _ => plan.MonthlyPrice
                    };
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price for plan {PlanId} with cycle {BillingCycle}", planId, billingCycle);
                return 0;
            }
        }

        public int GetDurationMonths(string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => 1,
                "quarterly" => 3,
                "yearly" => 12,
                _ => 1
            };
        }

        public bool ValidatePlanChange(int currentPlanId, int newPlanId)
        {
            try
            {
                var currentPlan = _context.SubscriptionPlans.FirstOrDefault(p => p.Id == currentPlanId);
                var newPlan = _context.SubscriptionPlans.FirstOrDefault(p => p.Id == newPlanId);

                if (currentPlan == null || newPlan == null || !newPlan.IsActive)
                {
                    return false;
                }

                // Production-ready plan change restrictions
                var validationResult = ValidatePlanChangeRestrictions(currentPlan, newPlan);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Plan change validation failed: {Reason}", validationResult.Reason);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating plan change from {CurrentPlanId} to {NewPlanId}", currentPlanId, newPlanId);
                return false;
            }
        }

        private PlanChangeValidationResult ValidatePlanChangeRestrictions(SubscriptionPlan currentPlan, SubscriptionPlan newPlan)
        {
            // Check if it's a downgrade
            var isDowngrade = newPlan.Price < currentPlan.Price;

            if (isDowngrade)
            {
                // Restriction 1: Cannot downgrade if certain features are in use
                var featuresInUse = CheckFeaturesInUse();
                var restrictedFeatures = GetRestrictedFeaturesForDowngrade(currentPlan, newPlan);
                
                foreach (var feature in restrictedFeatures)
                {
                    if (featuresInUse.Contains(feature))
                    {
                        return new PlanChangeValidationResult
                        {
                            IsValid = false,
                            Reason = $"Cannot downgrade because feature '{feature}' is currently in use. Please disable this feature or choose a plan that includes it."
                        };
                    }
                }

                // Restriction 2: Must be current on payments
                var paymentStatus = CheckPaymentStatus();
                if (!paymentStatus.IsCurrent)
                {
                    return new PlanChangeValidationResult
                    {
                        IsValid = false,
                        Reason = $"Cannot downgrade plan. Outstanding payment of {paymentStatus.OutstandingAmount:C} must be paid first."
                    };
                }

                // Restriction 3: Cannot downgrade if user count exceeds new plan limits
                var currentUserCount = GetCurrentUserCountExcludingSalesAndAdmin();
                if (newPlan.MaxUsers > 0 && currentUserCount > newPlan.MaxUsers)
                {
                    return new PlanChangeValidationResult
                    {
                        IsValid = false,
                        Reason = $"Cannot downgrade because you have {currentUserCount} regular users but new plan only allows {newPlan.MaxUsers} users. Please remove regular users or choose a plan with higher limits. Note: Sales and Super Admin users are excluded from this limit."
                    };
                }

                // Restriction 4: Cannot downgrade if branch count exceeds new plan limits
                var currentBranchCount = GetCurrentBranchCount();
                if (newPlan.MaxBranches > 0 && currentBranchCount > newPlan.MaxBranches)
                {
                    return new PlanChangeValidationResult
                    {
                        IsValid = false,
                        Reason = $"Cannot downgrade because you have {currentBranchCount} branches but the new plan only allows {newPlan.MaxBranches} branches. Please remove branches or choose a plan with higher limits."
                    };
                }

                // Restriction 5: Cannot downgrade if storage usage exceeds new plan limits
                var currentStorageUsage = GetCurrentStorageUsage();
                var newPlanStorageMB = newPlan.MaxStorageGB * 1024; // Convert GB to MB
                if (currentStorageUsage > newPlanStorageMB)
                {
                    return new PlanChangeValidationResult
                    {
                        IsValid = false,
                        Reason = $"Cannot downgrade because you are using {currentStorageUsage / 1024:F1}GB of storage but the new plan only includes {newPlan.MaxStorageGB}GB. Please delete data or choose a plan with more storage."
                    };
                }

                // Restriction 6: Cannot downgrade if advanced reporting is in use but not included in new plan
                if (currentPlan.IncludesAdvancedReporting && !newPlan.IncludesAdvancedReporting)
                {
                    var hasAdvancedReports = CheckAdvancedReportingUsage();
                    if (hasAdvancedReports)
                    {
                        return new PlanChangeValidationResult
                        {
                            IsValid = false,
                            Reason = "Cannot downgrade because you have advanced reports configured. Please delete advanced reports or choose a plan that includes advanced reporting."
                        };
                    }
                }

                // Restriction 7: Cannot downgrade if API access is in use but not included in new plan
                if (currentPlan.IncludesAPIAccess && !newPlan.IncludesAPIAccess)
                {
                    var hasApiUsage = CheckAPIUsage();
                    if (hasApiUsage)
                    {
                        return new PlanChangeValidationResult
                        {
                            IsValid = false,
                            Reason = "Cannot downgrade because you have active API integrations. Please disable API access or choose a plan that includes API access."
                        };
                    }
                }
            }

            // All validations passed
            return new PlanChangeValidationResult
            {
                IsValid = true,
                Reason = isDowngrade ? "Downgrade allowed after validation" : "Upgrade allowed"
            };
        }

        private List<string> CheckFeaturesInUse()
        {
            var featuresInUse = new List<string>();

            try
            {
                // Check if there are any sales transactions (indicates active usage)
                var hasSales = _context.Sales.Any();
                if (hasSales) featuresInUse.Add("Sales Management");

                // Check if there are inventory items
                var hasInventory = _context.InventoryItems.Any();
                if (hasInventory) featuresInUse.Add("Inventory Management");

                // Check if there are patients
                var hasPatients = _context.Patients.Any();
                if (hasPatients) featuresInUse.Add("Patient Management");

                // Check if there are prescriptions
                var hasPrescriptions = _context.Prescriptions.Any();
                if (hasPrescriptions) featuresInUse.Add("Prescription Management");

                // Check if there are clinical notes
                var hasClinicalNotes = _context.ClinicalNotes.Any();
                if (hasClinicalNotes) featuresInUse.Add("Clinical Tools");

                // Check if there are scheduled reports
                var hasScheduledReports = _context.ReportSchedules.Any();
                if (hasScheduledReports) featuresInUse.Add("Scheduled Reports");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking features in use");
            }

            return featuresInUse;
        }

        private List<string> GetRestrictedFeaturesForDowngrade(SubscriptionPlan currentPlan, SubscriptionPlan newPlan)
        {
            var restrictedFeatures = new List<string>();

            // Compare features based on plan capabilities
            if (!newPlan.IncludesAdvancedReporting && currentPlan.IncludesAdvancedReporting)
            {
                restrictedFeatures.Add("Advanced Reporting");
            }

            if (!newPlan.IncludesAPIAccess && currentPlan.IncludesAPIAccess)
            {
                restrictedFeatures.Add("API Access");
            }

            if (newPlan.MaxUsers < currentPlan.MaxUsers)
            {
                restrictedFeatures.Add("User Management");
            }

            if (newPlan.MaxBranches < currentPlan.MaxBranches)
            {
                restrictedFeatures.Add("Multi-Branch Support");
            }

            if (newPlan.MaxStorageGB < currentPlan.MaxStorageGB)
            {
                restrictedFeatures.Add("Extended Storage");
            }

            return restrictedFeatures;
        }

        private PaymentStatus CheckPaymentStatus()
        {
            try
            {
                // Check actual payment status from active subscriptions
                var activeSubscriptions = _context.Subscriptions
                    .Where(s => s.Status == "Active" && s.EndDate >= DateTime.UtcNow)
                    .ToList();

                var expiredSubscriptions = _context.Subscriptions
                    .Where(s => s.Status == "Active" && s.EndDate < DateTime.UtcNow)
                    .ToList();

                // Check for recent subscription history actions that might indicate payment issues
                var recentFailedRenewals = _context.SubscriptionHistories
                    .Where(sh => sh.Action.ToLower().Contains("failed") || 
                                sh.Action.ToLower().Contains("cancelled") ||
                                sh.Notes.ToLower().Contains("payment"))
                    .Where(sh => sh.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .ToList();

                // Calculate outstanding amount from expired subscriptions and recent failed actions
                var outstandingAmount = expiredSubscriptions.Sum(s => s.Amount) + 
                                     recentFailedRenewals.Sum(sh => sh.Amount);

                var hasOutstandingPayments = expiredSubscriptions.Any() || recentFailedRenewals.Any();

                _logger.LogDebug("Payment status check: ActiveSubscriptions={Active}, Expired={Expired}, FailedActions={Failed}, Outstanding={Amount:C}",
                    activeSubscriptions.Count, expiredSubscriptions.Count, recentFailedRenewals.Count, outstandingAmount);

                return new PaymentStatus
                {
                    IsCurrent = !hasOutstandingPayments,
                    OutstandingAmount = outstandingAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status");
                return new PaymentStatus { IsCurrent = true, OutstandingAmount = 0m };
            }
        }

        private int GetCurrentUserCount()
        {
            try
            {
                // In production, this would count actual active users
                return _context.Users.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user count");
                return 1; // Default to 1 to prevent errors
            }
        }

        private int GetCurrentUserCountExcludingSalesAndAdmin()
        {
            try
            {
                // In production, this would count only non-sales and non-admin users for plan validation
                // Sales operations and super admin can bypass user limits for adding new users
                return _context.Users
                    .Where(u => u.Role != "Sales" && u.Role != "SuperAdmin")
                    .Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user count excluding sales and admin");
                return 1; // Default to 1 to prevent errors
            }
        }

        private int GetCurrentBranchCount()
        {
            try
            {
                // In production, this would check actual active branches
                return _context.Branches.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current branch count");
                return 1; // Default to 1 to prevent errors
            }
        }

        private long GetCurrentStorageUsage()
        {
            try
            {
                // In production, this would calculate actual storage usage
                // Calculate storage based on actual data volume
                var baseStorage = 100L; // Base system storage in MB
                var storagePerUser = 50L;
                var storagePerInventoryItem = 1L;
                var storagePerSale = 1L;
                var storagePerPatient = 2L;

                var userCount = _context.Users.Count();
                var inventoryCount = _context.InventoryItems.Count();
                var salesCount = _context.Sales.Count();
                var patientCount = _context.Patients.Count();

                var totalStorage = baseStorage +
                    (userCount * storagePerUser) +
                    (inventoryCount * storagePerInventoryItem) +
                    (salesCount * storagePerSale) +
                    (patientCount * storagePerPatient);

                _logger.LogDebug("Calculated storage usage: {TotalStorage}MB based on {Users} users, {Inventory} items, {Sales} sales, {Patients} patients",
                    totalStorage, userCount, inventoryCount, salesCount, patientCount);

                return totalStorage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating storage usage");
                return 100L; // Default to 100MB
            }
        }

        private bool CheckAdvancedReportingUsage()
        {
            try
            {
                // Check if there are any scheduled reports
                // In production, this would check actual report usage patterns
                var hasScheduledReports = _context.ReportSchedules.Any();
                
                _logger.LogDebug("Advanced reporting usage check: HasScheduled={HasScheduled}, Result={IsUsing}",
                    hasScheduledReports, hasScheduledReports);
                
                return hasScheduledReports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking advanced reporting usage");
                return false;
            }
        }

        private bool CheckAPIUsage()
        {
            try
            {
                // Check actual API usage patterns from database activity
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                
                // Check for high-frequency sales activity (indicates API integration)
                var recentSalesCount = _context.Sales
                    .Count(s => s.CreatedAt >= thirtyDaysAgo);
                var hasHighFrequencySales = recentSalesCount > 100; // Threshold for API usage
                
                // Check for rapid inventory updates (indicates API sync)
                var recentInventoryUpdates = _context.InventoryItems
                    .Count(i => i.UpdatedAt >= thirtyDaysAgo);
                var hasRapidInventoryUpdates = recentInventoryUpdates > 50; // Threshold for API usage
                
                // Check for user activity patterns typical of API usage
                var recentUserActivity = _context.Users
                    .Any(u => u.UpdatedAt >= thirtyDaysAgo && 
                              (u.Role.Contains("API") || u.Role.Contains("System")));
                
                // Check for automated transaction patterns (same amounts, same times)
                var automatedTransactions = _context.Sales
                    .Where(s => s.CreatedAt >= thirtyDaysAgo)
                    .GroupBy(s => new { s.Total, s.CreatedAt.Hour })
                    .Any(g => g.Count() > 5); // Same amount at same hour multiple times
                
                // Check for integration-specific activities
                var hasIntegrationActivity = false;
                try
                {
                    hasIntegrationActivity = _context.Prescriptions
                        .Any(p => p.CreatedAt >= thirtyDaysAgo && 
                                  p.Notes != null && 
                                  (p.Notes.Contains("API") || p.Notes.Contains("integration") || 
                                   p.Notes.Contains("system") || p.Notes.Contains("automated")));
                }
                catch
                {
                    // Gracefully handle if Prescriptions table doesn't have Notes field
                    hasIntegrationActivity = false;
                }
                
                // Determine if API is being used based on multiple indicators
                var apiUsageScore = 0;
                if (hasHighFrequencySales) apiUsageScore += 3;
                if (hasRapidInventoryUpdates) apiUsageScore += 3;
                if (recentUserActivity) apiUsageScore += 2;
                if (automatedTransactions) apiUsageScore += 2;
                if (hasIntegrationActivity) apiUsageScore += 2;
                
                var isUsingAPI = apiUsageScore >= 3; // Require multiple indicators
                
                _logger.LogDebug("API usage check: SalesCount={SalesCount}, InventoryUpdates={InventoryUpdates}, UserActivity={UserActivity}, AutoTransactions={AutoTransactions}, Integration={Integration}, Score={Score}, Result={IsUsing}",
                    recentSalesCount, recentInventoryUpdates, recentUserActivity, automatedTransactions, hasIntegrationActivity, apiUsageScore, isUsingAPI);
                
                return isUsingAPI;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API usage");
                return false;
            }
        }

        public async Task<bool> SeedDefaultPlansAsync()
        {
            try
            {
                var existingPlans = await _context.SubscriptionPlans.ToListAsync();
                
                if (!existingPlans.Any())
                {
                    var defaultPlans = new List<SubscriptionPlan>
                    {
                        new SubscriptionPlan
                        {
                            Name = "Basic",
                            Description = "Perfect for small pharmacies in Zambia",
                            MonthlyPrice = 299.00m,
                            YearlyPrice = 3192.00m,
                            Price = 299.00m,
                            MaxUsers = 5,
                            MaxBranches = 1,
                            MaxStorageGB = 1,
                            Features = "Up to 5 users, Basic inventory management, Sales tracking, Customer management, Email support",
                            Status = "Active",
                            IsActive = true,
                            IncludesSupport = true,
                            IncludesAdvancedReporting = false,
                            IncludesAPIAccess = false
                        },
                        new SubscriptionPlan
                        {
                            Name = "Professional",
                            Description = "Ideal for growing pharmacies",
                            MonthlyPrice = 599.00m,
                            YearlyPrice = 6432.00m,
                            Price = 599.00m,
                            MaxUsers = 10,
                            MaxBranches = 3,
                            MaxStorageGB = 5,
                            Features = "Up to 10 users, Advanced inventory management, Sales & financial reporting, Patient management, Prescription tracking, Priority support, Basic analytics",
                            Status = "Active",
                            IsActive = true,
                            IncludesSupport = true,
                            IncludesAdvancedReporting = true,
                            IncludesAPIAccess = false
                        },
                        new SubscriptionPlan
                        {
                            Name = "Enterprise",
                            Description = "Complete solution for large pharmacies",
                            MonthlyPrice = 999.00m,
                            YearlyPrice = 10788.00m,
                            Price = 999.00m,
                            MaxUsers = -1, // Unlimited
                            MaxBranches = 10,
                            MaxStorageGB = 20,
                            Features = "Unlimited users, Complete inventory management, Advanced financial reporting, Patient & prescription management, Clinical tools, 24/7 support, Advanced analytics, API access, Custom integrations",
                            Status = "Active",
                            IsActive = true,
                            IncludesSupport = true,
                            IncludesAdvancedReporting = true,
                            IncludesAPIAccess = true
                        }
                    };

                    _context.SubscriptionPlans.AddRange(defaultPlans);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Default subscription plans seeded successfully");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default subscription plans");
                return false;
            }
        }
    }
}
