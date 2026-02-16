using System;
using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.DTOs
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int MaxUsers { get; set; }

        public int MaxBranches { get; set; }

        public int MaxStorageGB { get; set; }

        public string Features { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int TenantCount { get; set; }

        public string[] FeatureList => Features?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    }

    public class CreateSubscriptionPlanRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Range(1, 9999)]
        public int MaxUsers { get; set; }

        [Range(1, 999)]
        public int MaxBranches { get; set; }

        [Range(1, 1000)]
        public int MaxStorageGB { get; set; }

        [StringLength(500)]
        public string Features { get; set; } = string.Empty;
    }

    public class UpdateSubscriptionPlanRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Range(1, 9999)]
        public int MaxUsers { get; set; }

        [Range(1, 999)]
        public int MaxBranches { get; set; }

        [Range(1, 1000)]
        public int MaxStorageGB { get; set; }

        [StringLength(500)]
        public string Features { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class SubscriptionPlanStatsDto
    {
        public int TotalPlans { get; set; }

        public int ActivePlans { get; set; }

        public int InactivePlans { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalTenants { get; set; }
    }

    public class SubscriptionPlanFeatureDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Basic { get; set; }

        public bool Professional { get; set; }

        public bool Enterprise { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }

    public class UpdateFeatureRequest
    {
        public int FeatureId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Basic { get; set; }

        public bool Professional { get; set; }

        public bool Enterprise { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }

    public class UpdateStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
