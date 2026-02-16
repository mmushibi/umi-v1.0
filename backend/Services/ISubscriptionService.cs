using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface ISubscriptionService
    {
        Task<List<Subscription>> GetSubscriptionsAsync();
        Task<Subscription> GetSubscriptionAsync(string id);
        Task<Subscription> CreateSubscriptionAsync(CreateSubscriptionRequest request);
        Task<Subscription> UpdateSubscriptionAsync(string id, UpdateSubscriptionRequest request);
        Task<bool> DeleteSubscriptionAsync(string id);
        Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync();
        Task<SubscriptionPlan> GetSubscriptionPlanAsync(int id);
        Task<SubscriptionPlan> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request);
        Task<SubscriptionPlan> UpdateSubscriptionPlanAsync(int id, UpdateSubscriptionPlanRequest request);
        Task<bool> DeleteSubscriptionPlanAsync(int id);
        Task<List<Pharmacy>> SearchPharmaciesAsync(string query);
        Task<List<Pharmacy>> SearchPharmaciesByPhoneAsync(string query);
        Task<List<Payment>> GetSubscriptionPaymentsAsync(string subscriptionId);
        Task<List<ActivityLog>> GetSubscriptionActivityLogAsync(string subscriptionId);
        Task<SubscriptionStats> GetSubscriptionStatsAsync();
    }

    public class CreateSubscriptionRequest
    {
        public int PlanId { get; set; }
        public string PharmacyId { get; set; }
        public DateTime StartDate { get; set; }
        public string BillingCycle { get; set; } // "monthly", "quarterly", "annually"
        public bool AutoRenew { get; set; } = true;
    }

    public class UpdateSubscriptionRequest
    {
        public int? PlanId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string BillingCycle { get; set; }
        public bool? AutoRenew { get; set; }
        public string Status { get; set; }
    }

    public class CreateSubscriptionPlanRequest
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public int MaxStorageGB { get; set; }
        public string Features { get; set; }
    }

    public class UpdateSubscriptionPlanRequest
    {
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxBranches { get; set; }
        public int? MaxStorageGB { get; set; }
        public string Features { get; set; }
        public bool? IsActive { get; set; }
    }

    public class Payment
    {
        public string Id { get; set; }
        public string SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Description { get; set; }
    }

    public class SubscriptionStats
    {
        public int ActiveSubscriptions { get; set; }
        public int ExpiringSoonCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
