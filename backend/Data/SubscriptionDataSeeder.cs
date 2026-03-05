using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public class SubscriptionDataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionDataSeeder> _logger;

        public SubscriptionDataSeeder(
            ApplicationDbContext context,
            ILogger<SubscriptionDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedSubscriptionDataAsync()
        {
            try
            {
                // Check if subscription plans already exist
                if (await _context.SubscriptionPlans.AnyAsync())
                {
                    _logger.LogInformation("Subscription plans already exist");
                    return;
                }

                // Create subscription plans only
                var plans = new[]
                {
                    new SubscriptionPlan
                    {
                        Name = "basic",
                        MonthlyPrice = 299m,
                        YearlyPrice = 2990m,
                        Price = 299m,
                        MaxUsers = 5,
                        MaxBranches = 1,
                        MaxProducts = 500,
                        MaxTransactions = 1000,
                        MaxStorageGB = 5,
                        Features = "Basic inventory management, sales tracking, reporting",
                        IsActive = true,
                        IncludesSupport = false,
                        IncludesAdvancedReporting = false,
                        IncludesAPIAccess = false,
                        Status = "Active"
                    },
                    new SubscriptionPlan
                    {
                        Name = "professional",
                        MonthlyPrice = 599m,
                        YearlyPrice = 5990m,
                        Price = 599m,
                        MaxUsers = 15,
                        MaxBranches = 3,
                        MaxProducts = 2000,
                        MaxTransactions = 5000,
                        MaxStorageGB = 10,
                        Features = "Advanced inventory, multi-branch, advanced reporting, API access",
                        IsActive = true,
                        IncludesSupport = true,
                        IncludesAdvancedReporting = true,
                        IncludesAPIAccess = true,
                        Status = "Active"
                    },
                    new SubscriptionPlan
                    {
                        Name = "enterprise",
                        MonthlyPrice = 999m,
                        YearlyPrice = 9990m,
                        Price = 999m,
                        MaxUsers = 50,
                        MaxBranches = 10,
                        MaxProducts = 10000,
                        MaxTransactions = 20000,
                        MaxStorageGB = 50,
                        Features = "Full feature set, priority support, custom integrations, unlimited storage",
                        IsActive = true,
                        IncludesSupport = true,
                        IncludesAdvancedReporting = true,
                        IncludesAPIAccess = true,
                        Status = "Active"
                    }
                };

                await _context.SubscriptionPlans.AddRangeAsync(plans);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription plans seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding subscription data");
                throw;
            }
        }
    }
}
