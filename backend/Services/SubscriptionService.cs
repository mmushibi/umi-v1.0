using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(ApplicationDbContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Subscription>> GetSubscriptionsAsync()
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.Pharmacy)
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions");
                throw;
            }
        }

        public async Task<Subscription> GetSubscriptionAsync(string id)
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.Pharmacy)
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            try
            {
                // Validate pharmacy exists
                var pharmacy = await _context.Pharmacies.FindAsync(request.PharmacyId);
                if (pharmacy == null)
                {
                    throw new ArgumentException("Pharmacy not found");
                }

                // Validate plan exists
                var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
                if (plan == null || !plan.IsActive)
                {
                    throw new ArgumentException("Subscription plan not found or inactive");
                }

                // Calculate end date based on billing cycle
                var endDate = CalculateEndDate(request.StartDate, request.BillingCycle);

                var subscription = new Subscription
                {
                    PlanId = request.PlanId,
                    PharmacyId = request.PharmacyId,
                    StartDate = request.StartDate,
                    EndDate = endDate,
                    Amount = CalculateAmount(plan.Price, request.BillingCycle),
                    Status = "active",
                    AutoRenew = request.AutoRenew,
                    IsActive = true
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription {SubscriptionId} for pharmacy {PharmacyId} with plan {PlanId}",
                    subscription.Id, request.PharmacyId, request.PlanId);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                throw;
            }
        }

        public async Task<Subscription> UpdateSubscriptionAsync(string id, UpdateSubscriptionRequest request)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);
                if (subscription == null)
                {
                    throw new ArgumentException("Subscription not found");
                }

                // Update fields if provided
                if (request.PlanId.HasValue)
                {
                    var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId.Value);
                    if (plan == null || !plan.IsActive)
                    {
                        throw new ArgumentException("Subscription plan not found or inactive");
                    }
                    subscription.PlanId = request.PlanId.Value;
                }

                if (request.StartDate.HasValue)
                {
                    subscription.StartDate = request.StartDate.Value;
                }

                if (request.EndDate.HasValue)
                {
                    subscription.EndDate = request.EndDate.Value;
                }

                if (!string.IsNullOrEmpty(request.BillingCycle))
                {
                    subscription.EndDate = CalculateEndDate(subscription.StartDate, request.BillingCycle);
                }

                if (request.AutoRenew.HasValue)
                {
                    subscription.AutoRenew = request.AutoRenew.Value;
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    subscription.Status = request.Status;
                }

                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription {SubscriptionId}", id);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteSubscriptionAsync(string id)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(id);
                if (subscription == null)
                {
                    return false;
                }

                subscription.IsActive = false;
                subscription.Status = "cancelled";
                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted subscription {SubscriptionId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync()
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
                throw;
            }
        }

        public async Task<SubscriptionPlan> GetSubscriptionPlanAsync(int id)
        {
            try
            {
                return await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plan with ID: {Id}", id);
                throw;
            }
        }

        public async Task<SubscriptionPlan> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request)
        {
            try
            {
                var plan = new SubscriptionPlan
                {
                    Name = request.Name,
                    Price = request.Price,
                    MaxUsers = request.MaxUsers,
                    MaxBranches = request.MaxBranches,
                    MaxStorageGB = request.MaxStorageGB,
                    Features = request.Features,
                    IsActive = true
                };

                _context.SubscriptionPlans.Add(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription plan {PlanId} with name {Name}", plan.Id, plan.Name);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription plan");
                throw;
            }
        }

        public async Task<SubscriptionPlan> UpdateSubscriptionPlanAsync(int id, UpdateSubscriptionPlanRequest request)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    throw new ArgumentException("Subscription plan not found");
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Name))
                {
                    plan.Name = request.Name;
                }

                if (request.Price.HasValue)
                {
                    plan.Price = request.Price.Value;
                }

                if (request.MaxUsers.HasValue)
                {
                    plan.MaxUsers = request.MaxUsers.Value;
                }

                if (request.MaxBranches.HasValue)
                {
                    plan.MaxBranches = request.MaxBranches.Value;
                }

                if (request.MaxStorageGB.HasValue)
                {
                    plan.MaxStorageGB = request.MaxStorageGB.Value;
                }

                if (!string.IsNullOrEmpty(request.Features))
                {
                    plan.Features = request.Features;
                }

                if (request.IsActive.HasValue)
                {
                    plan.IsActive = request.IsActive.Value;
                }

                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription plan {PlanId}", id);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteSubscriptionPlanAsync(int id)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return false;
                }

                plan.IsActive = false;
                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted subscription plan {PlanId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription plan with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<Pharmacy>> SearchPharmaciesAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return new List<Pharmacy>();
                }

                return await _context.Pharmacies
                    .Where(p => p.IsActive &&
                               (p.Name.ToLower().Contains(query.ToLower()) ||
                                p.Email.ToLower().Contains(query.ToLower())))
                    .OrderBy(p => p.Name)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching pharmacies with query: {Query}", query);
                throw;
            }
        }

        public async Task<List<Pharmacy>> SearchPharmaciesByPhoneAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return new List<Pharmacy>();
                }

                return await _context.Pharmacies
                    .Where(p => p.IsActive && p.Phone.Contains(query))
                    .OrderBy(p => p.Name)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching pharmacies by phone with query: {Query}", query);
                throw;
            }
        }

        public async Task<List<Payment>> GetSubscriptionPaymentsAsync(string subscriptionId)
        {
            try
            {
                // For now, return empty list as payment system is not implemented
                // In a real implementation, this would query a Payment table
                return new List<Payment>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<List<ActivityLog>> GetSubscriptionActivityLogAsync(string subscriptionId)
        {
            try
            {
                return await _context.ActivityLogs
                    .Where(a => a.Description.Contains($"subscription:{subscriptionId}") ||
                               a.Description.Contains($"Subscription {subscriptionId}"))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity log for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }

        public async Task<SubscriptionStats> GetSubscriptionStatsAsync()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.IsActive)
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var thirtyDaysFromNow = today.AddDays(30);

                var activeSubscriptions = subscriptions.Count(s => s.Status == "active");
                var expiringSoon = subscriptions.Count(s =>
                    s.Status == "active" && s.EndDate <= thirtyDaysFromNow);

                var monthlyRevenue = subscriptions
                    .Where(s => s.Status == "active")
                    .Sum(s => s.Amount);

                return new SubscriptionStats
                {
                    ActiveSubscriptions = activeSubscriptions,
                    ExpiringSoonCount = expiringSoon,
                    MonthlyRevenue = monthlyRevenue,
                    TotalSubscriptions = subscriptions.Count,
                    TotalRevenue = subscriptions.Sum(s => s.Amount)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription stats");
                throw;
            }
        }

        private DateTime CalculateEndDate(DateTime startDate, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => startDate.AddMonths(1),
                "quarterly" => startDate.AddMonths(3),
                "annually" => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
        }

        private decimal CalculateAmount(decimal basePrice, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => basePrice,
                "quarterly" => basePrice * 3,
                "annually" => basePrice * 12,
                _ => basePrice
            };
        }
    }
}
