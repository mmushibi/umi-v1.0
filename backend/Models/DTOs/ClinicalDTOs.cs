using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models.DTOs
{
    public class DrugInteraction
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Medication1 { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Medication2 { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Recommendation { get; set; } = string.Empty;
    }

    public class MedicationAllergy
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Medication { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Allergen { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Reaction { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Recommendation { get; set; } = string.Empty;
    }

    public class AllergyCheckResult
    {
        public bool HasAllergies { get; set; }
        public List<MedicationAllergy> Allergies { get; set; } = new List<MedicationAllergy>();
    }

    public class DosageCalculation
    {
        [Required]
        [StringLength(100)]
        public string Medication { get; set; } = string.Empty;
        
        public decimal Weight { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        
        [Required]
        [StringLength(200)]
        public string RecommendedDosage { get; set; } = string.Empty;
        
        public decimal TotalDailyDose { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Frequency { get; set; } = string.Empty;
        
        public decimal MaximumDose { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class ClinicalGuideline
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Condition { get; set; }
        
        [StringLength(100)]
        public string? Medication { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Recommendation { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string EvidenceLevel { get; set; } = string.Empty;
        
        public DateTime LastUpdated { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Source { get; set; } = string.Empty;
    }

    public class VitalSignsReference
    {
        public int Age { get; set; }
        public int BloodPressureSystolicMin { get; set; }
        public int BloodPressureSystolicMax { get; set; }
        public int BloodPressureDiastolicMin { get; set; }
        public int BloodPressureDiastolicMax { get; set; }
        public int HeartRateMin { get; set; }
        public int HeartRateMax { get; set; }
        public int RespiratoryRateMin { get; set; }
        public int RespiratoryRateMax { get; set; }
        public decimal TemperatureMin { get; set; }
        public decimal TemperatureMax { get; set; }
        public int OxygenSaturationMin { get; set; }
        public int OxygenSaturationMax { get; set; }
        public string ClinicalNotes { get; set; } = string.Empty;
    }

    public class CreateClinicalNoteRequest
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        [StringLength(50)]
        public string NoteType { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Diagnosis { get; set; }

        [StringLength(1000)]
        public string? Symptoms { get; set; }

        [StringLength(1000)]
        public string? Treatment { get; set; }

        public bool FollowUpRequired { get; set; } = false;
        public DateTime? FollowUpDate { get; set; }
    }
}
