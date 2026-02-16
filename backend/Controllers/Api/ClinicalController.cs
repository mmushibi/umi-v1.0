using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClinicalController : ControllerBase
    {
        private readonly ILogger<ClinicalController> _logger;
        private readonly ApplicationDbContext _context;

        public ClinicalController(
            ILogger<ClinicalController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ?? "";
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirst("TenantId")?.Value ?? "";
        }

        [HttpGet("drug-interactions")]
        public async Task<ActionResult<IEnumerable<DrugInteraction>>> GetDrugInteractions([FromQuery] string medications)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                if (string.IsNullOrWhiteSpace(medications))
                {
                    return BadRequest(new { error = "Medications parameter is required" });
                }

                var medicationList = medications.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToLower())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                if (medicationList.Count < 2)
                {
                    return BadRequest(new { error = "At least 2 medications are required for interaction check" });
                }

                // Mock drug interaction data - in production, this would integrate with a clinical database
                var interactions = await GetMockDrugInteractions(medicationList);

                return Ok(interactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking drug interactions for medications: {Medications}", medications);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("allergy-check")]
        public async Task<ActionResult<AllergyCheckResult>> CheckAllergies([FromQuery] string medications, [FromQuery] string allergies)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                if (string.IsNullOrWhiteSpace(medications))
                {
                    return BadRequest(new { error = "Medications parameter is required" });
                }

                var medicationList = medications.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToLower())
                    .ToList();

                var allergyList = string.IsNullOrWhiteSpace(allergies) 
                    ? new List<string>()
                    : allergies.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim().ToLower())
                        .ToList();

                // Mock allergy check - in production, this would integrate with a clinical database
                var result = await GetMockAllergyCheck(medicationList, allergyList);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking allergies for medications: {Medications}, allergies: {Allergies}", medications, allergies);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dosage-calculator")]
        public async Task<ActionResult<DosageCalculation>> CalculateDosage(
            [FromQuery] string medication,
            [FromQuery] decimal weight,
            [FromQuery] int age,
            [FromQuery] string? gender = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                if (string.IsNullOrWhiteSpace(medication) || weight <= 0 || age <= 0)
                {
                    return BadRequest(new { error = "Medication, weight, and age are required parameters" });
                }

                // Mock dosage calculation - in production, this would use clinical guidelines
                var calculation = await GetMockDosageCalculation(medication, weight, age, gender);

                return Ok(calculation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating dosage for medication: {Medication}, weight: {Weight}, age: {Age}", 
                    medication, weight, age);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("clinical-guidelines")]
        public async Task<ActionResult<IEnumerable<ClinicalGuideline>>> GetClinicalGuidelines(
            [FromQuery] string? condition = null,
            [FromQuery] string? medication = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                // Mock clinical guidelines - in production, this would integrate with clinical databases
                var guidelines = await GetMockClinicalGuidelines(condition, medication);

                return Ok(guidelines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinical guidelines for condition: {Condition}, medication: {Medication}", 
                    condition, medication);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("vital-signs-reference")]
        public async Task<ActionResult<VitalSignsReference>> GetVitalSignsReference([FromQuery] int age)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                if (age <= 0)
                {
                    return BadRequest(new { error = "Valid age is required" });
                }

                // Mock vital signs reference - in production, this would use clinical guidelines
                var reference = await GetMockVitalSignsReference(age);

                return Ok(reference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vital signs reference for age: {Age}", age);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("clinical-notes")]
        public async Task<ActionResult<ClinicalNote>> CreateClinicalNote([FromBody] CreateClinicalNoteRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify patient exists and belongs to tenant
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.TenantId == tenantId && p.IsActive);

                if (patient == null)
                {
                    return BadRequest(new { error = "Patient not found" });
                }

                var clinicalNote = new ClinicalNote
                {
                    PatientId = request.PatientId,
                    NoteType = request.NoteType,
                    Content = request.Content,
                    Diagnosis = request.Diagnosis,
                    Symptoms = request.Symptoms,
                    Treatment = request.Treatment,
                    FollowUpRequired = request.FollowUpRequired,
                    FollowUpDate = request.FollowUpDate,
                    TenantId = tenantId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ClinicalNotes.Add(clinicalNote);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clinical note {NoteId} created for patient {PatientId} by user {UserId}", 
                    clinicalNote.Id, request.PatientId, userId);

                return CreatedAtAction(nameof(GetClinicalNote), new { id = clinicalNote.Id }, clinicalNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating clinical note");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("clinical-notes/{id}")]
        public async Task<ActionResult<ClinicalNote>> GetClinicalNote(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var note = await _context.ClinicalNotes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);

                if (note == null)
                {
                    return NotFound(new { error = "Clinical note not found" });
                }

                return Ok(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinical note {NoteId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("patients/{patientId}/clinical-notes")]
        public async Task<ActionResult<IEnumerable<ClinicalNote>>> GetPatientClinicalNotes(
            int patientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                // Verify patient exists and belongs to tenant
                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId && p.IsActive);

                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                var query = _context.ClinicalNotes
                    .AsNoTracking()
                    .Where(n => n.PatientId == patientId && n.TenantId == tenantId);

                var totalCount = await query.CountAsync();
                var notes = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    notes,
                    totalCount,
                    currentPage = page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinical notes for patient {PatientId}", patientId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Mock data methods - in production, these would integrate with real clinical databases
        private async Task<List<DrugInteraction>> GetMockDrugInteractions(List<string> medications)
        {
            await Task.Delay(50); // Simulate API call

            var interactions = new List<DrugInteraction>();

            // Mock interaction data
            if (medications.Contains("warfarin") && medications.Contains("aspirin"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Warfarin",
                    Medication2 = "Aspirin",
                    Severity = "Major",
                    Description = "Increased risk of bleeding",
                    Recommendation = "Avoid concurrent use if possible"
                });
            }

            if (medications.Contains("lisinopril") && medications.Contains("potassium"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Lisinopril",
                    Medication2 = "Potassium",
                    Severity = "Moderate",
                    Description = "Increased risk of hyperkalemia",
                    Recommendation = "Monitor potassium levels"
                });
            }

            return interactions;
        }

        private async Task<AllergyCheckResult> GetMockAllergyCheck(List<string> medications, List<string> allergies)
        {
            await Task.Delay(50); // Simulate API call

            var result = new AllergyCheckResult
            {
                HasAllergies = false,
                Allergies = new List<MedicationAllergy>()
            };

            // Mock allergy data
            if (allergies.Any(a => a.Contains("penicillin")) && medications.Any(m => m.Contains("amoxicillin")))
            {
                result.HasAllergies = true;
                result.Allergies.Add(new MedicationAllergy
                {
                    Medication = "Amoxicillin",
                    Allergen = "Penicillin",
                    Severity = "Severe",
                    Reaction = "Anaphylaxis",
                    Recommendation = "Avoid all penicillin-based antibiotics"
                });
            }

            return result;
        }

        private async Task<DosageCalculation> GetMockDosageCalculation(string medication, decimal weight, int age, string? gender)
        {
            await Task.Delay(50); // Simulate calculation

            var calculation = new DosageCalculation
            {
                Medication = medication,
                Weight = weight,
                Age = age,
                Gender = gender,
                RecommendedDosage = "10mg/kg/day",
                TotalDailyDose = weight * 10,
                Frequency = "Twice daily",
                MaximumDose = 1000,
                Notes = "Adjust for renal impairment if necessary"
            };

            return calculation;
        }

        private async Task<List<ClinicalGuideline>> GetMockClinicalGuidelines(string? condition, string? medication)
        {
            await Task.Delay(50); // Simulate API call

            var guidelines = new List<ClinicalGuideline>();

            if (!string.IsNullOrEmpty(condition) && condition.ToLower().Contains("hypertension"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Hypertension Management",
                    Condition = "Hypertension",
                    Recommendation = "Start with ACE inhibitor or thiazide diuretic",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-30)
                });
            }

            if (!string.IsNullOrEmpty(medication) && medication.ToLower().Contains("metformin"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Type 2 Diabetes - Metformin",
                    Medication = "Metformin",
                    Recommendation = "First-line therapy for type 2 diabetes",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-60)
                });
            }

            return guidelines;
        }

        private async Task<VitalSignsReference> GetMockVitalSignsReference(int age)
        {
            await Task.Delay(50); // Simulate API call

            var reference = new VitalSignsReference
            {
                Age = age,
                BloodPressureSystolicMin = age < 18 ? 90 : 110,
                BloodPressureSystolicMax = age < 18 ? 120 : 140,
                BloodPressureDiastolicMin = age < 18 ? 60 : 70,
                BloodPressureDiastolicMax = age < 18 ? 80 : 90,
                HeartRateMin = age < 1 ? 100 : age < 18 ? 60 : 50,
                HeartRateMax = age < 1 ? 160 : age < 18 ? 100 : 90,
                RespiratoryRateMin = age < 1 ? 30 : age < 18 ? 12 : 12,
                RespiratoryRateMax = age < 1 ? 60 : age < 18 ? 20 : 20,
                TemperatureMin = 36.5m,
                TemperatureMax = 37.5m
            };

            return reference;
        }
    }

    // Response DTOs
    public class DrugInteraction
    {
        public string Medication1 { get; set; } = string.Empty;
        public string Medication2 { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class AllergyCheckResult
    {
        public bool HasAllergies { get; set; }
        public List<MedicationAllergy> Allergies { get; set; } = new List<MedicationAllergy>();
    }

    public class MedicationAllergy
    {
        public string Medication { get; set; } = string.Empty;
        public string Allergen { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Reaction { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class DosageCalculation
    {
        public string Medication { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public string RecommendedDosage { get; set; } = string.Empty;
        public decimal TotalDailyDose { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public decimal MaximumDose { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class ClinicalGuideline
    {
        public string Title { get; set; } = string.Empty;
        public string? Condition { get; set; }
        public string? Medication { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string EvidenceLevel { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
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
    }

    // Request DTOs
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
