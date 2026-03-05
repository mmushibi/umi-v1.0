using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DrugInteractionModel = UmiHealthPOS.Models.DrugInteraction;
using ClinicalGuidelineModel = UmiHealthPOS.Models.ClinicalGuideline;

namespace UmiHealthPOS.Services
{
    public interface IClinicalService
    {
        Task<List<DrugInteractionModel>> GetDrugInteractionsAsync(List<string> medications);
        Task<AllergyCheckResult> CheckAllergiesAsync(List<string> medications, List<string> allergies);
        Task<DosageCalculation> CalculateDosageAsync(string medication, decimal weight, int age, string? gender);
        Task<List<ClinicalGuidelineModel>> GetClinicalGuidelinesAsync(string? condition, string? medication);
        Task<VitalSignsReference> GetVitalSignsReferenceAsync(int age);
        Task SeedClinicalDataAsync();
    }

    public class ClinicalService : IClinicalService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClinicalService> _logger;

        public ClinicalService(ApplicationDbContext context, ILogger<ClinicalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DrugInteractionModel>> GetDrugInteractionsAsync(List<string> medications)
        {
            try
            {
                var interactions = new List<DrugInteractionModel>();
                var medsLower = medications.Select(m => m.ToLower()).ToList();

                for (int i = 0; i < medsLower.Count; i++)
                {
                    for (int j = i + 1; j < medsLower.Count; j++)
                    {
                        var dbInteractions = await _context.DrugInteractions
                            .Where(di => 
                                (di.Medication1.ToLower().Contains(medsLower[i]) || di.Medication2.ToLower().Contains(medsLower[i])) &&
                                (di.Medication1.ToLower().Contains(medsLower[j]) || di.Medication2.ToLower().Contains(medsLower[j])))
                            .ToListAsync();

                        interactions.AddRange(dbInteractions);
                    }
                }

                if (!interactions.Any())
                {
                    _logger.LogInformation("No drug interactions found for medications: {Medications}", string.Join(", ", medications));
                }

                return interactions.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drug interactions for medications: {Medications}", string.Join(", ", medications));
                return new List<DrugInteractionModel>();
            }
        }

        public async Task<AllergyCheckResult> CheckAllergiesAsync(List<string> medications, List<string> allergies)
        {
            try
            {
                var result = new AllergyCheckResult
                {
                    HasAllergies = false,
                    Allergies = new List<Models.DTOs.MedicationAllergy>()
                };

                var medsLower = medications.Select(m => m.ToLower()).ToList();
                var allergiesLower = allergies.Select(a => a.ToLower()).ToList();

                foreach (var allergen in allergiesLower)
                {
                    var medicationAllergies = await _context.MedicationAllergies
                        .Where(ma => 
                            ma.Allergen.ToLower().Contains(allergen) &&
                            medsLower.Any(med => ma.Medication.ToLower().Contains(med)))
                        .Select(ma => new Models.DTOs.MedicationAllergy
                        {
                            Id = ma.Id,
                            Medication = ma.Medication,
                            Allergen = ma.Allergen,
                            Severity = ma.Severity,
                            Reaction = ma.Reaction ?? ma.ReactionType,
                            Recommendation = ma.Recommendation
                        })
                        .ToListAsync();

                    if (medicationAllergies.Any())
                    {
                        result.HasAllergies = true;
                        result.Allergies.AddRange(medicationAllergies);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking allergies for medications: {Medications}, allergies: {Allergies}", 
                    string.Join(", ", medications), string.Join(", ", allergies));
                return new AllergyCheckResult { HasAllergies = false, Allergies = new List<UmiHealthPOS.Models.DTOs.MedicationAllergy>() };
            }
        }

        public async Task<DosageCalculation> CalculateDosageAsync(string medication, decimal weight, int age, string? gender)
        {
            try
            {
                var calculation = new DosageCalculation
                {
                    Medication = medication,
                    Weight = weight,
                    Age = age,
                    Gender = gender,
                    RecommendedDosage = "Standard dosage",
                    TotalDailyDose = weight * 2,
                    Frequency = "Twice daily",
                    MaximumDose = weight * 3,
                    Notes = "Consult healthcare provider for specific dosing"
                };

                return calculation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating dosage for medication: {Medication}", medication);
                return new DosageCalculation();
            }
        }

        public async Task<List<ClinicalGuidelineModel>> GetClinicalGuidelinesAsync(string? condition, string? medication)
        {
            try
            {
                var guidelines = await _context.ClinicalGuidelines
                    .Where(g => string.IsNullOrEmpty(condition) || g.Condition.ToLower().Contains(condition.ToLower()))
                    .Where(g => string.IsNullOrEmpty(medication) || g.Medication.ToLower().Contains(medication.ToLower()))
                    .ToListAsync();

                return guidelines.Select(g => new ClinicalGuidelineModel
                {
                    Id = g.Id,
                    Title = g.Title,
                    Condition = g.Condition,
                    Medication = g.Medication,
                    Recommendation = g.Recommendation ?? g.Recommendations ?? string.Empty,
                    EvidenceLevel = g.EvidenceLevel,
                    Source = g.Source,
                    LastUpdated = g.UpdatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinical guidelines for condition: {Condition}, medication: {Medication}", 
                    condition, medication);
                return new List<ClinicalGuidelineModel>();
            }
        }

        public async Task<VitalSignsReference> GetVitalSignsReferenceAsync(int age)
        {
            await Task.CompletedTask; // Make method properly async
            try
            {
                var reference = new VitalSignsReference
                {
                    Age = age,
                    BloodPressureSystolicMin = 90,
                    BloodPressureSystolicMax = 140,
                    BloodPressureDiastolicMin = 60,
                    BloodPressureDiastolicMax = 90,
                    HeartRateMin = 60,
                    HeartRateMax = 100,
                    RespiratoryRateMin = 12,
                    RespiratoryRateMax = 20,
                    TemperatureMin = 36.0m,
                    TemperatureMax = 37.5m,
                    OxygenSaturationMin = 95,
                    OxygenSaturationMax = 100,
                    ClinicalNotes = "Normal vital signs for this age group"
                };

                return reference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vital signs reference for age: {Age}", age);
                return new VitalSignsReference();
            }
        }

        public async Task SeedClinicalDataAsync()
        {
            try
            {
                _logger.LogInformation("Seeding clinical data");

                var existingInteractions = await _context.DrugInteractions.AnyAsync();
                if (!existingInteractions)
                {
                    var interactions = new List<UmiHealthPOS.Models.DrugInteraction>
                    {
                        new UmiHealthPOS.Models.DrugInteraction { Medication1 = "Warfarin", Medication2 = "Aspirin", Severity = "Major", Description = "Increased bleeding risk", Recommendation = "Avoid concurrent use" },
                        new UmiHealthPOS.Models.DrugInteraction { Medication1 = "Simvastatin", Medication2 = "Clarithromycin", Severity = "Major", Description = "Rhabdomyolysis risk", Recommendation = "Avoid concurrent use" }
                    };
                    _context.DrugInteractions.AddRange(interactions);
                }

                var existingGuidelines = await _context.ClinicalGuidelines.AnyAsync();
                if (!existingGuidelines)
                {
                    var guidelines = new List<UmiHealthPOS.Models.ClinicalGuideline>
                    {
                        new UmiHealthPOS.Models.ClinicalGuideline { Title = "Hypertension Management", Condition = "Hypertension", Recommendation = "Start with ACE inhibitor", EvidenceLevel = "Grade A", Source = "WHO Guidelines" },
                        new UmiHealthPOS.Models.ClinicalGuideline { Title = "Diabetes Management", Condition = "Diabetes", Recommendation = "Metformin first-line", EvidenceLevel = "Grade A", Source = "ADA Guidelines" }
                    };
                    _context.ClinicalGuidelines.AddRange(guidelines);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Clinical data seeding completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding clinical data");
                throw;
            }
        }
    }
}
