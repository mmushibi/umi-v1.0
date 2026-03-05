using System;

namespace UmiHealthPOS.DTOs
{
    public class AllergyCheckResultDto
    {
        public bool HasAllergies { get; set; }
        public string[] Allergies { get; set; } = Array.Empty<string>();
        public string Severity { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;
    }

    public class AllergyCheckResult
    {
        public bool HasAllergies { get; set; }
        public string[] Allergies { get; set; } = Array.Empty<string>();
        public string Severity { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;
    }

    public class DosageCalculationDto
    {
        public string MedicationName { get; set; } = string.Empty;
        public decimal RecommendedDosage { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal PatientWeight { get; set; }
        public int PatientAge { get; set; }
        public string CalculationMethod { get; set; } = string.Empty;
    }

    public class DosageCalculation
    {
        public string MedicationName { get; set; } = string.Empty;
        public decimal RecommendedDosage { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal PatientWeight { get; set; }
        public int PatientAge { get; set; }
        public string CalculationMethod { get; set; } = string.Empty;
    }

    public class VitalSignsReferenceDto
    {
        public int AgeMin { get; set; }
        public int AgeMax { get; set; }
        public string AgeGroup { get; set; } = string.Empty;
        public decimal HeartRateMin { get; set; }
        public decimal HeartRateMax { get; set; }
        public decimal SystolicBPMin { get; set; }
        public decimal SystolicBPMax { get; set; }
        public decimal DiastolicBPMin { get; set; }
        public decimal DiastolicBPMax { get; set; }
        public decimal RespiratoryRateMin { get; set; }
        public decimal RespiratoryRateMax { get; set; }
        public decimal TemperatureMin { get; set; }
        public decimal TemperatureMax { get; set; }
        public decimal OxygenSaturationMin { get; set; }
        public decimal OxygenSaturationMax { get; set; }
        public string ClinicalNotes { get; set; } = string.Empty;
    }

    public class VitalSignsReference
    {
        public int AgeMin { get; set; }
        public int AgeMax { get; set; }
        public string AgeGroup { get; set; } = string.Empty;
        public decimal HeartRateMin { get; set; }
        public decimal HeartRateMax { get; set; }
        public decimal SystolicBPMin { get; set; }
        public decimal SystolicBPMax { get; set; }
        public decimal DiastolicBPMin { get; set; }
        public decimal DiastolicBPMax { get; set; }
        public decimal RespiratoryRateMin { get; set; }
        public decimal RespiratoryRateMax { get; set; }
        public decimal TemperatureMin { get; set; }
        public decimal TemperatureMax { get; set; }
        public decimal OxygenSaturationMin { get; set; }
        public decimal OxygenSaturationMax { get; set; }
        public string ClinicalNotes { get; set; } = string.Empty;
    }

    public class CreateClinicalNoteRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int PatientId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(2000)]
        public string Notes { get; set; } = string.Empty;
        
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string? Type { get; set; }
        
        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string? Category { get; set; }
        
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string? Diagnosis { get; set; }
        
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string? Treatment { get; set; }
        
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string? FollowUpInstructions { get; set; }
    }
}
