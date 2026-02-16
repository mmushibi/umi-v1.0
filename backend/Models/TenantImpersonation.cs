using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models
{
    public class TenantImpersonation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string AdminUserId { get; set; } = string.Empty;

        [Required]
        public int TenantId { get; set; }

        [Required]
        [StringLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Ended, Expired

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("AdminUserId")]
        public virtual User AdminUser { get; set; } = null!;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
    }

    public class ImpersonationLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string AdminUserId { get; set; } = string.Empty;

        [Required]
        public int TenantId { get; set; }

        [Required]
        [StringLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Started, Stopped, Failed

        [Required]
        public DateTime Timestamp { get; set; }

        [StringLength(100)]
        public string Duration { get; set; } = string.Empty;

        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Success"; // Success, Failed

        [StringLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("AdminUserId")]
        public virtual User AdminUser { get; set; } = null!;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
    }

    public class TenantImpersonationRequest
    {
        [Required]
        public int TenantId { get; set; }

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class TenantImpersonationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ImpersonationToken { get; set; }
        public string? TenantDashboardUrl { get; set; }
        public DateTime? StartTime { get; set; }
    }

    public class TenantImpersonationStats
    {
        public int TotalTenants { get; set; }
        public int ActiveSessions { get; set; }
        public int TodaySessions { get; set; }
        public int MonthlyLogs { get; set; }
    }

    public class TenantImpersonationDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Logo { get; set; } = string.Empty;
    }
}
