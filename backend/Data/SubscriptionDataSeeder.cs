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
                // Check if data already exists
                if (await _context.SubscriptionHistories.AnyAsync())
                {
                    _logger.LogInformation("Subscription history data already exists");
                    return;
                }

                // Create subscription plans
                var plans = new[]
                {
                    new SubscriptionPlan
                    {
                        Name = "basic",
                        Price = 1350m,
                        MaxUsers = 5,
                        MaxBranches = 1,
                        MaxStorageGB = 5,
                        Features = "Basic inventory management, sales tracking, reporting",
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Name = "professional",
                        Price = 4050m,
                        MaxUsers = 15,
                        MaxBranches = 3,
                        MaxStorageGB = 10,
                        Features = "Advanced inventory, multi-branch, advanced reporting, API access",
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Name = "enterprise",
                        Price = 13500m,
                        MaxUsers = 50,
                        MaxBranches = 10,
                        MaxStorageGB = 50,
                        Features = "Full feature set, priority support, custom integrations, unlimited storage",
                        IsActive = true
                    }
                };

                await _context.SubscriptionPlans.AddRangeAsync(plans);
                await _context.SaveChangesAsync();

                // Create pharmacies
                var pharmacies = new[]
                {
                    new Pharmacy
                    {
                        Name = "MediCare Pharmacy",
                        Email = "admin@medicare.com",
                        Phone = "+260 977 123456",
                        Address = "Plot 1234, Cairo Road, Lusaka",
                        City = "Lusaka",
                        Province = "Lusaka",
                        PostalCode = "10101",
                        Country = "Zambia",
                        IsActive = true
                    },
                    new Pharmacy
                    {
                        Name = "HealthPlus Group",
                        Email = "info@healthplus.com",
                        Phone = "+260 966 789012",
                        Address = "Stand 567, Great East Road, Lusaka",
                        City = "Lusaka",
                        Province = "Lusaka",
                        PostalCode = "10101",
                        Country = "Zambia",
                        IsActive = true
                    },
                    new Pharmacy
                    {
                        Name = "City Hospital",
                        Email = "contact@cityhospital.com",
                        Phone = "+260 211 345678",
                        Address = "Corner of Church Road, Lusaka",
                        City = "Lusaka",
                        Province = "Lusaka",
                        PostalCode = "10101",
                        Country = "Zambia",
                        IsActive = true
                    },
                    new Pharmacy
                    {
                        Name = "Quick Pharmacy",
                        Email = "support@quickpharmacy.com",
                        Phone = "+260 955 234567",
                        Address = "Shop 12, Manda Hill, Lusaka",
                        City = "Lusaka",
                        Province = "Lusaka",
                        PostalCode = "10101",
                        Country = "Zambia",
                        IsActive = true
                    }
                };

                await _context.Pharmacies.AddRangeAsync(pharmacies);
                await _context.SaveChangesAsync();

                // Create users
                var users = new[]
                {
                    new UserAccount
                    {
                        UserId = "john-doe-001",
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "john.doe@umihealth.com",
                        PhoneNumber = "+260 977 111111",
                        Role = "TenantAdmin",
                        IsActive = true
                    },
                    new UserAccount
                    {
                        UserId = "jane-smith-002",
                        FirstName = "Jane",
                        LastName = "Smith",
                        Email = "jane.smith@umihealth.com",
                        PhoneNumber = "+260 977 222222",
                        Role = "TenantAdmin",
                        IsActive = true
                    }
                };

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();

                // Create subscriptions
                var basicPlan = plans.First(p => p.Name == "basic");
                var professionalPlan = plans.First(p => p.Name == "professional");
                var enterprisePlan = plans.First(p => p.Name == "enterprise");

                var subscriptions = new[]
                {
                    new Subscription
                    {
                        PlanId = basicPlan.Id,
                        PharmacyId = pharmacies.First(p => p.Name == "MediCare Pharmacy").Id,
                        StartDate = DateTime.Now.AddDays(-30),
                        EndDate = DateTime.Now.AddDays(30),
                        Amount = 1350m,
                        Status = "active",
                        IsActive = true
                    },
                    new Subscription
                    {
                        PlanId = professionalPlan.Id,
                        PharmacyId = pharmacies.First(p => p.Name == "HealthPlus Group").Id,
                        StartDate = DateTime.Now.AddDays(-25),
                        EndDate = DateTime.Now.AddDays(35),
                        Amount = 4050m,
                        Status = "active",
                        IsActive = true
                    },
                    new Subscription
                    {
                        PlanId = enterprisePlan.Id,
                        PharmacyId = pharmacies.First(p => p.Name == "City Hospital").Id,
                        StartDate = DateTime.Now.AddDays(-35),
                        EndDate = DateTime.Now.AddDays(25),
                        Amount = 13500m,
                        Status = "active",
                        IsActive = true
                    },
                    new Subscription
                    {
                        PlanId = basicPlan.Id,
                        PharmacyId = pharmacies.First(p => p.Name == "Quick Pharmacy").Id,
                        StartDate = DateTime.Now.AddDays(-20),
                        EndDate = DateTime.Now.AddDays(10),
                        Amount = 1350m,
                        Status = "cancelled",
                        IsActive = false
                    }
                };

                await _context.Subscriptions.AddRangeAsync(subscriptions);
                await _context.SaveChangesAsync();

                // Create subscription history
                var history = new[]
                {
                    new SubscriptionHistory
                    {
                        SubscriptionId = subscriptions.FirstOrDefault(s => s.Pharmacy?.Name == "MediCare Pharmacy")?.Id ?? 0,
                        Action = "created",
                        PreviousPlan = "",
                        NewPlan = "basic",
                        Amount = 1350m,
                        Notes = "New subscription created",
                        UserId = users.FirstOrDefault(u => u.FirstName == "John")?.Id.ToString() ?? "",
                        CreatedAt = DateTime.Now.AddDays(-15)
                    },
                    new SubscriptionHistory
                    {
                        SubscriptionId = subscriptions.FirstOrDefault(s => s.Pharmacy?.Name == "HealthPlus Group")?.Id ?? 0,
                        Action = "created",
                        PreviousPlan = "",
                        NewPlan = "professional",
                        Amount = 4050m,
                        Notes = "Professional plan setup",
                        CreatedAt = DateTime.Now.AddDays(-5)
                    },
                    new SubscriptionHistory
                    {
                        SubscriptionId = subscriptions.FirstOrDefault(s => s.Pharmacy?.Name == "MediCare Pharmacy")?.Id ?? 0,
                        Action = "upgraded",
                        PreviousPlan = "basic",
                        NewPlan = "professional",
                        Amount = 4050m,
                        Notes = "Upgraded from Basic",
                        UserId = users.FirstOrDefault(u => u.FirstName == "Jane")?.UserId.ToString() ?? "",
                        CreatedAt = DateTime.Now.AddDays(-12)
                    },
                    new SubscriptionHistory
                    {
                        SubscriptionId = subscriptions.FirstOrDefault(s => s.Pharmacy?.Name == "HealthPlus Group")?.Id ?? 0,
                        Action = "renewed",
                        PreviousPlan = "professional",
                        NewPlan = "professional",
                        Amount = 4050m,
                        Notes = "Monthly renewal",
                        UserId = users.FirstOrDefault(u => u.FirstName == "John")?.Id.ToString() ?? "",
                        CreatedAt = DateTime.Now.AddDays(-18)
                    }
                };

                await _context.SubscriptionHistories.AddRangeAsync(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription data seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding subscription data");
                throw;
            }
        }
    }
}
