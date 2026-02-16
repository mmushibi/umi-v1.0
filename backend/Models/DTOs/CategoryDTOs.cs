using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int TenantUsage { get; set; }
        public string? RequestedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
    }

    public class CreateCategoryRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string ColorClass { get; set; } = "bg-blue-100 text-blue-600";
    }

    public class UpdateCategoryRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string ColorClass { get; set; } = "bg-blue-100 text-blue-600";
    }

    public class CategoryApprovalRequest
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
    }

    public class CategoryStatusRequest
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty; // "Active" or "Inactive"
    }

    public class CategorySyncRequest
    {
        public List<int> CategoryIds { get; set; } = new();
        public List<string> TenantIds { get; set; } = new();
    }

    public class CategoryStatsDto
    {
        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int PendingCategories { get; set; }
        public int InactiveCategories { get; set; }
        public int TenantUsage { get; set; }
    }

    public class CategoryUsageDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public int TenantCount { get; set; }
        public int ItemCount { get; set; }
        public List<TenantUsageDetail> TenantUsages { get; set; } = new();
    }

    public class TenantUsageDetail
    {
        public string TenantId { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTime LastUsedAt { get; set; }
    }
}
