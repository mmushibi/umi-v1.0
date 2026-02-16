using System;
using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.DTOs
{
    public class ApplicationFeatureDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Module { get; set; } = string.Empty;

        public bool BasicPlan { get; set; }

        public bool ProfessionalPlan { get; set; }

        public bool EnterprisePlan { get; set; }

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class CreateApplicationFeatureRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(20)]
        public string Module { get; set; } = string.Empty;

        public bool BasicPlan { get; set; } = false;

        public bool ProfessionalPlan { get; set; } = false;

        public bool EnterprisePlan { get; set; } = false;

        public int SortOrder { get; set; } = 0;
    }

    public class UpdateApplicationFeatureRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(20)]
        public string Module { get; set; } = string.Empty;

        public bool BasicPlan { get; set; } = false;

        public bool ProfessionalPlan { get; set; } = false;

        public bool EnterprisePlan { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }

    public class UpdateFeaturePlanRequest
    {
        public int FeatureId { get; set; }

        public bool BasicPlan { get; set; }

        public bool ProfessionalPlan { get; set; }

        public bool EnterprisePlan { get; set; }
    }

    public class ApplicationFeatureStatsDto
    {
        public int TotalFeatures { get; set; }

        public int ActiveFeatures { get; set; }

        public int InactiveFeatures { get; set; }

        public int BasicPlanFeatures { get; set; }

        public int ProfessionalPlanFeatures { get; set; }

        public int EnterprisePlanFeatures { get; set; }
    }
}
