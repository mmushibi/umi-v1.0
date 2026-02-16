using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.DTOs
{
    public class ControlledSubstanceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime LastDispensed { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ComplianceScore { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public DateTime LastAudit { get; set; }
        public DateTime NextAuditDue { get; set; }
        public int MonthlyDispensed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Tenant information
        public string PharmacyName { get; set; } = string.Empty;
        public string PharmacyEmail { get; set; } = string.Empty;
        public string PharmacyLicense { get; set; } = string.Empty;
        public string PharmacyPhone { get; set; } = string.Empty;
        public string PharmacyAvatar { get; set; } = string.Empty;
    }

    public class CreateControlledSubstanceRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string GenericName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Schedule { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        public int CurrentStock { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RegistrationNumber { get; set; } = string.Empty;
    }

    public class UpdateControlledSubstanceRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string GenericName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Schedule { get; set; } = string.Empty;

        [Required]
        public int CurrentStock { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RegistrationNumber { get; set; } = string.Empty;
    }

    public class ControlledSubstanceAuditDto
    {
        public int Id { get; set; }
        public int SubstanceId { get; set; }
        public string AuditorName { get; set; } = string.Empty;
        public string Finding { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? DiscrepancyDetails { get; set; }
        public string? RequiredAction { get; set; }
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public DateTime AuditDate { get; set; }

        // Calculated property
        public string Impact { get; set; } = string.Empty;
    }

    public class CreateAuditRequest
    {
        [Required]
        public int CurrentStock { get; set; }

        [Required]
        [StringLength(50)]
        public string Finding { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? DiscrepancyDetails { get; set; }

        [StringLength(500)]
        public string? RequiredAction { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool ProperStorage { get; set; }
        public bool AccessLog { get; set; }
        public bool Documentation { get; set; }
        public bool Security { get; set; }
    }

    public class ComplianceReportRequest
    {
        [Required]
        public int SubstanceId { get; set; }
    }

    public class ComplianceReportData
    {
        public ControlledSubstanceDto Substance { get; set; } = new();
        public string ReportDate { get; set; } = string.Empty;
        public string ReportId { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public int ComplianceScore { get; set; }
        public List<ControlledSubstanceAuditDto> AuditHistory { get; set; } = new();
        public List<RecommendationDto> Recommendations { get; set; } = new();
        public RiskAssessmentDto RiskAssessment { get; set; } = new();
    }

    public class RecommendationDto
    {
        public string Priority { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RiskAssessmentDto
    {
        public string Level { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<string> Factors { get; set; } = new();
    }

    public class TenantDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public string? ZambiaRegNumber { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Avatar { get; set; } = string.Empty;
    }
}
