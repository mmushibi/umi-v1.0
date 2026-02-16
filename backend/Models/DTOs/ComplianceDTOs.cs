using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models.DTOs
{
    // Main compliance status DTO
    public class ComplianceStatusDto
    {
        public int OverallScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public List<ComplianceAreaStatusDto> Areas { get; set; } = new();
        public string TenantId { get; set; } = string.Empty;
        public string? PharmacyName { get; set; }
        public string? LicenseNumber { get; set; }
    }

    // Individual compliance area status
    public class ComplianceAreaStatusDto
    {
        public string Area { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    // Compliance area information
    public class ComplianceAreaDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public DateTime NextCheck { get; set; }
        public List<string> Sources { get; set; } = new();
    }

    // Detailed compliance information
    public class ComplianceDetailDto
    {
        public string Area { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public DateTime NextReview { get; set; }
        public List<string> Sources { get; set; } = new();
    }

    // Compliance updates/notifications
    public class ComplianceUpdateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime DatePosted { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool ActionRequired { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    // License validation request
    public class LicenseValidationRequest
    {
        [Required]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        public string LicenseType { get; set; } = string.Empty; // Pharmacy, Pharmacist, etc.

        public string? TenantId { get; set; }
    }

    // License validation response
    public class LicenseValidationResponse
    {
        public bool IsValid { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public string? HolderName { get; set; }
        public List<string> Issues { get; set; } = new();
        public DateTime ValidatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    // Compliance report request
    public class ComplianceReportRequest
    {
        [Required]
        public string TenantId { get; set; } = string.Empty;

        public string? Area { get; set; } // Specific area or null for all

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IncludeDetails { get; set; } = false;

        public bool IncludeRecommendations { get; set; } = true;
    }

    // Compliance report data
    public class ComplianceReportData
    {
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int OverallScore { get; set; }
        public string OverallStatus { get; set; } = string.Empty;
        public List<ComplianceAreaStatusDto> AreaDetails { get; set; } = new();
        public List<ComplianceUpdateDto> RecentUpdates { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, int> ScoreHistory { get; set; } = new();
        public string GeneratedBy { get; set; } = string.Empty;
    }

    // Compliance metrics
    public class ComplianceMetricsDto
    {
        public int TotalAreas { get; set; }
        public int CompliantAreas { get; set; }
        public int PartiallyCompliantAreas { get; set; }
        public int NonCompliantAreas { get; set; }
        public double CompliancePercentage { get; set; }
        public DateTime LastAssessment { get; set; }
        public DateTime NextAssessment { get; set; }
        public List<string> CriticalIssues { get; set; } = new();
        public List<string> UpcomingDeadlines { get; set; } = new();
    }

    // Compliance action item
    public class ComplianceActionItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
        public string Status { get; set; } = string.Empty; // Pending, InProgress, Completed, Overdue
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Compliance audit log
    public class ComplianceAuditLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // Check, Update, Violation, etc.
        public string Description { get; set; } = string.Empty;
        public string PreviousScore { get; set; } = string.Empty;
        public string NewScore { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Source { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Regulatory update subscription
    public class RegulatorySubscriptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new(); // ZAMRA, PSZ, ZRA, etc.
        public List<string> Categories { get; set; } = new(); // Regulatory, Tax, Safety, etc.
        public List<string> Priorities { get; set; } = new(); // High, Medium, Low
        public bool EmailNotifications { get; set; } = true;
        public bool InAppNotifications { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
