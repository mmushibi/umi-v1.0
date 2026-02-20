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

        // Enhanced mock data methods - Production Integration Ready
        // These methods provide comprehensive clinical data and are ready for integration with:
        // - FDA Drug Interactions API (https://open.fda.gov/drug/interaction)
        // - Micromedex (https://www.micromedexsolutions.com/)
        // - Epocrates (https://www.epocrates.com/)
        // - WHO Essential Medicines Database
        // - Zambia Ministry of Health Clinical Guidelines
        // 
        // Integration Strategy:
        // 1. Replace mock data with real API calls
        // 2. Add proper API authentication (API keys, OAuth)
        // 3. Implement caching for frequently accessed data
        // 4. Add error handling for API failures
        // 5. Include Zambia-specific clinical protocols
        private async Task<List<DrugInteraction>> GetMockDrugInteractions(List<string> medications)
        {
            await Task.Delay(50); // Simulate API call

            var interactions = new List<DrugInteraction>();
            var medsLower = medications.Select(m => m.ToLower()).ToList();

            // Major interactions - life threatening
            if (medsLower.Contains("warfarin") && medsLower.Contains("aspirin"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Warfarin",
                    Medication2 = "Aspirin",
                    Severity = "Major",
                    Description = "Increased risk of major bleeding due to combined antiplatelet and anticoagulant effects",
                    Recommendation = "Avoid concurrent use. Consider alternative anticoagulation or antiplatelet therapy."
                });
            }

            if (medsLower.Contains("simvastatin") && medsLower.Contains("clarithromycin"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Simvastatin",
                    Medication2 = "Clarithromycin",
                    Severity = "Major",
                    Description = "Increased risk of rhabdomyolysis due to CYP3A4 inhibition",
                    Recommendation = "Avoid concurrent use. Consider alternative antibiotic or statin."
                });
            }

            // Moderate interactions - require monitoring
            if (medsLower.Contains("lisinopril") && (medsLower.Contains("potassium") || medsLower.Contains("spironolactone")))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Lisinopril",
                    Medication2 = medsLower.Contains("potassium") ? "Potassium" : "Spironolactone",
                    Severity = "Moderate",
                    Description = "Increased risk of hyperkalemia due to reduced potassium excretion",
                    Recommendation = "Monitor serum potassium levels and renal function regularly."
                });
            }

            if (medsLower.Contains("metformin") && medsLower.Contains("contrast"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Metformin",
                    Medication2 = "Iodinated Contrast Media",
                    Severity = "Moderate",
                    Description = "Increased risk of lactic acidosis in patients with renal impairment",
                    Recommendation = "Temporarily discontinue metformin at time of contrast procedure."
                });
            }

            // Minor interactions - limited clinical significance
            if (medsLower.Contains("amoxicillin") && medsLower.Contains("allopurinol"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Amoxicillin",
                    Medication2 = "Allopurinol",
                    Severity = "Minor",
                    Description = "Increased risk of allergic reactions",
                    Recommendation = "Monitor for signs of allergic reaction. Consider alternative antibiotic if needed."
                });
            }

            // Common Zambian medication interactions
            if (medsLower.Contains("artemether") && medsLower.Contains("lumefantrine"))
            {
                interactions.Add(new DrugInteraction
                {
                    Medication1 = "Artemether",
                    Medication2 = "Lumefantrine",
                    Severity = "Minor",
                    Description = "Standard co-formulation for malaria treatment",
                    Recommendation = "No action needed - this is the intended combination therapy."
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

            var medsLower = medications.Select(m => m.ToLower()).ToList();
            var allergiesLower = allergies.Select(a => a.ToLower()).ToList();

            // Penicillin cross-reactivity
            if (allergiesLower.Any(a => a.Contains("penicillin")) &&
                medsLower.Any(m => m.Contains("amoxicillin") || m.Contains("ampicillin") || m.Contains("cloxacillin")))
            {
                result.HasAllergies = true;
                var conflictingMed = medsLower.FirstOrDefault(m =>
                    m.Contains("amoxicillin") || m.Contains("ampicillin") || m.Contains("cloxacillin"));

                result.Allergies.Add(new MedicationAllergy
                {
                    Medication = conflictingMed?.ToUpper() ?? "Penicillin-based antibiotic",
                    Allergen = "Penicillin",
                    Severity = "Severe",
                    Reaction = "Anaphylaxis possible",
                    Recommendation = "Avoid all penicillin-based antibiotics. Consider macrolides or quinolones."
                });
            }

            // Sulfa allergy
            if (allergiesLower.Any(a => a.Contains("sulfa") || a.Contains("sulfonamide")) &&
                medsLower.Any(m => m.Contains("sulfamethoxazole") || m.Contains("trimethoprim") || m.Contains("co-trimoxazole")))
            {
                result.HasAllergies = true;
                result.Allergies.Add(new MedicationAllergy
                {
                    Medication = "Co-trimoxazole (Bactrim)",
                    Allergen = "Sulfonamide",
                    Severity = "Moderate to Severe",
                    Reaction = "Rash, fever, possible Stevens-Johnson syndrome",
                    Recommendation = "Avoid sulfonamide antibiotics. Consider alternative for UTI or respiratory infections."
                });
            }

            // NSAID allergy
            if (allergiesLower.Any(a => a.Contains("aspirin") || a.Contains("nsaid")) &&
                medsLower.Any(m => m.Contains("ibuprofen") || m.Contains("naproxen") || m.Contains("diclofenac")))
            {
                result.HasAllergies = true;
                var conflictingMed = medsLower.FirstOrDefault(m =>
                    m.Contains("ibuprofen") || m.Contains("naproxen") || m.Contains("diclofenac"));

                result.Allergies.Add(new MedicationAllergy
                {
                    Medication = conflictingMed?.ToUpper() ?? "NSAID",
                    Allergen = "Aspirin/NSAID",
                    Severity = "Moderate",
                    Reaction = "Bronchospasm, urticaria, angioedema",
                    Recommendation = "Avoid all NSAIDs. Consider acetaminophen or COX-2 inhibitors with caution."
                });
            }

            // Codeine allergy (common in Zambia)
            if (allergiesLower.Any(a => a.Contains("codeine")) &&
                medsLower.Any(m => m.Contains("codeine") || m.Contains("tramadol") || m.Contains("morphine")))
            {
                result.HasAllergies = true;
                result.Allergies.Add(new MedicationAllergy
                {
                    Medication = "Opioid analgesic",
                    Allergen = "Codeine",
                    Severity = "Moderate",
                    Reaction = "Nausea, constipation, respiratory depression",
                    Recommendation = "Avoid opioids. Consider non-opioid analgesics or alternative pain management."
                });
            }

            // Quinolone allergy
            if (allergiesLower.Any(a => a.Contains("quinolone") || a.Contains("ciprofloxacin")) &&
                medsLower.Any(m => m.Contains("ciprofloxacin") || m.Contains("levofloxacin") || m.Contains("ofloxacin")))
            {
                result.HasAllergies = true;
                result.Allergies.Add(new MedicationAllergy
                {
                    Medication = "Fluoroquinolone antibiotic",
                    Allergen = "Quinolone",
                    Severity = "Moderate",
                    Reaction = "Tendon rupture, photosensitivity, QT prolongation",
                    Recommendation = "Avoid fluoroquinolones. Consider alternative antibiotic classes."
                });
            }

            return result;
        }

        private async Task<DosageCalculation> GetMockDosageCalculation(string medication, decimal weight, int age, string? gender)
        {
            await Task.Delay(50); // Simulate calculation

            var medLower = medication.ToLower();
            var calculation = new DosageCalculation
            {
                Medication = medication,
                Weight = weight,
                Age = age,
                Gender = gender
            };

            // Pediatric and adult dosing calculations based on common medications in Zambia
            if (medLower.Contains("amoxicillin"))
            {
                // Amoxicillin: 25-50 mg/kg/day divided BID for children, 500mg TID for adults
                if (age < 12)
                {
                    calculation.RecommendedDosage = "25-50 mg/kg/day divided twice daily";
                    calculation.TotalDailyDose = Math.Round(weight * 40, 0); // Using 40 mg/kg/day average
                    calculation.Frequency = "Twice daily";
                    calculation.MaximumDose = 1000; // Maximum for children
                    calculation.Notes = "Use 250mg/5mL suspension for children. Treat for 7-10 days.";
                }
                else
                {
                    calculation.RecommendedDosage = "500mg three times daily";
                    calculation.TotalDailyDose = 1500;
                    calculation.Frequency = "Three times daily";
                    calculation.MaximumDose = 1500;
                    calculation.Notes = "For adults. Adjust dose for renal impairment (CrCl <30mL/min).";
                }
            }
            else if (medLower.Contains("paracetamol") || medLower.Contains("acetaminophen"))
            {
                // Paracetamol: 10-15 mg/kg/dose Q6H for children, 500mg-1g Q6H for adults
                if (age < 12)
                {
                    calculation.RecommendedDosage = "10-15 mg/kg/dose every 6 hours";
                    var perDose = Math.Round(weight * 12.5m, 0); // Using 12.5 mg/kg average
                    calculation.TotalDailyDose = perDose * 4; // Q6H = 4 doses per day
                    calculation.Frequency = "Every 6 hours";
                    calculation.MaximumDose = 60; // Maximum per dose for children in mg/kg
                    calculation.Notes = "Maximum 60mg/kg/day. Do not exceed 4 doses in 24 hours.";
                }
                else
                {
                    calculation.RecommendedDosage = "500mg-1g every 6 hours";
                    calculation.TotalDailyDose = 4000; // Maximum daily dose
                    calculation.Frequency = "Every 6 hours";
                    calculation.MaximumDose = 1000; // Per dose maximum
                    calculation.Notes = "Maximum 4g/day. Use with caution in liver disease.";
                }
            }
            else if (medLower.Contains("ibuprofen"))
            {
                // Ibuprofen: 5-10 mg/kg/dose Q6-8H for children, 400-600mg Q6-8H for adults
                if (age < 12)
                {
                    calculation.RecommendedDosage = "5-10 mg/kg/dose every 6-8 hours";
                    var perDose = Math.Round(weight * 7.5m, 0); // Using 7.5 mg/kg average
                    calculation.TotalDailyDose = perDose * 3; // TID dosing
                    calculation.Frequency = "Every 6-8 hours";
                    calculation.MaximumDose = 40; // Maximum per dose for children in mg/kg
                    calculation.Notes = "Give with food. Avoid in dehydration or renal impairment.";
                }
                else
                {
                    calculation.RecommendedDosage = "400-600mg every 6-8 hours";
                    calculation.TotalDailyDose = 1800; // Maximum daily dose
                    calculation.Frequency = "Every 6-8 hours";
                    calculation.MaximumDose = 600; // Per dose maximum
                    calculation.Notes = "Maximum 1.2g/day OTC, 2.4g/day prescription. Take with food.";
                }
            }
            else if (medLower.Contains("metformin"))
            {
                // Metformin: Start 500mg daily, titrate to 2000mg/day max
                calculation.RecommendedDosage = "Start 500mg once daily with evening meal";
                calculation.TotalDailyDose = 500; // Starting dose
                calculation.Frequency = "Once daily (can increase to BID)";
                calculation.MaximumDose = 2000; // Maximum daily dose
                calculation.Notes = "Titrate every 1-2 weeks. Contraindicated in renal failure (eGFR <30).";
            }
            else if (medLower.Contains("artemether") || medLower.Contains("lumefantrine"))
            {
                // Artemether/Lumefantrine (Coartem): Weight-based dosing
                decimal dosePerKg = weight switch
                {
                    < 5 => 0, // Not recommended for <5kg
                    < 15 => 20, // 1 tablet per dose
                    < 25 => 40, // 2 tablets per dose
                    < 35 => 60, // 3 tablets per dose
                    _ => 80 // 4 tablets per dose
                };

                calculation.RecommendedDosage = $"Weight-based dosing: {dosePerKg}mg per dose";
                calculation.TotalDailyDose = dosePerKg * 2; // BID for 3 days
                calculation.Frequency = "Twice daily for 3 days";
                calculation.MaximumDose = 480; // Maximum total dose
                calculation.Notes = "Take with food. Complete full 3-day course even if feeling better.";
            }
            else
            {
                // Default calculation for other medications
                calculation.RecommendedDosage = "10mg/kg/day divided BID";
                calculation.TotalDailyDose = weight * 10;
                calculation.Frequency = "Twice daily";
                calculation.MaximumDose = 1000;
                calculation.Notes = "Adjust for renal impairment if necessary. Consult specific medication guidelines.";
            }

            // Age-specific adjustments
            if (age > 65)
            {
                calculation.Notes += " Consider dose reduction in elderly patients.";
            }

            if (weight < 10)
            {
                calculation.Notes += " Use pediatric formulation and precise dosing.";
            }

            return calculation;
        }

        // Enhanced clinical guidelines - in production, these would integrate with real clinical databases
        // such as WHO guidelines, Zambia Ministry of Health protocols, or UpToDate
        private async Task<List<ClinicalGuideline>> GetMockClinicalGuidelines(string? condition, string? medication)
        {
            await Task.Delay(50); // Simulate API call

            var guidelines = new List<ClinicalGuideline>();
            var conditionLower = condition?.ToLower() ?? "";
            var medicationLower = medication?.ToLower() ?? "";

            // Hypertension Management (WHO/ISH Guidelines)
            if (conditionLower.Contains("hypertension") || conditionLower.Contains("high blood pressure"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Hypertension Management - WHO/ISH Guidelines",
                    Condition = "Hypertension",
                    Recommendation = "Start with ACE inhibitor or calcium channel blocker. Target BP <140/90 mmHg. For patients >65 years, target <150/90 mmHg. Add thiazide diuretic if needed.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-30),
                    Source = "World Health Organization, International Society of Hypertension"
                });
            }

            // Type 2 Diabetes (ADA Guidelines)
            if (conditionLower.Contains("diabetes") || conditionLower.Contains("type 2 diabetes"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Type 2 Diabetes Management",
                    Condition = "Type 2 Diabetes",
                    Recommendation = "Metformin first-line unless contraindicated. Target HbA1c <7%. Consider GLP-1 agonist or SGLT2 inhibitor for patients with established CVD.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-45),
                    Source = "American Diabetes Association Standards of Care"
                });
            }

            // Malaria Treatment (Zambia Specific)
            if (conditionLower.Contains("malaria") || medicationLower.Contains("artemether"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Uncomplicated Malaria Treatment - Zambia Protocol",
                    Condition = "Malaria (Uncomplicated)",
                    Medication = "Artemether/Lumefantrine",
                    Recommendation = "First-line: Artemether/Lumefantrine 4mg/kg/dose twice daily for 3 days. Alternative: Dihydroartemisinin-piperaquine. Treat all confirmed cases.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-20),
                    Source = "Zambia Ministry of Health National Malaria Control Programme"
                });
            }

            // HIV Management (Zambia Guidelines)
            if (conditionLower.Contains("hiv") || conditionLower.Contains("aids"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "HIV Treatment - Zambia ART Guidelines",
                    Condition = "HIV Infection",
                    Recommendation = "Start ART regardless of CD4 count. First-line: TDF/3TC/DTG. Monitor viral load at 6 months, then annually. Provide TB prophylaxis if CD4 <350.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-60),
                    Source = "Zambia Ministry of Health HIV/AIDS Treatment Guidelines"
                });
            }

            // TB Treatment (Zambia Protocol)
            if (conditionLower.Contains("tuberculosis") || conditionLower.Contains("tb"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Tuberculosis Treatment - Zambia Protocol",
                    Condition = "Tuberculosis",
                    Recommendation = "2 months intensive phase (RHZE) + 4 months continuation phase (RH). Directly observed therapy (DOT) recommended. Monitor for side effects monthly.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-90),
                    Source = "Zambia National TB and Leprosy Programme"
                });
            }

            // Asthma Management (GINA Guidelines)
            if (conditionLower.Contains("asthma") || medicationLower.Contains("salbutamol"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Asthma Management - GINA Guidelines",
                    Condition = "Asthma",
                    Recommendation = "Step-wise approach. For mild intermittent: SABA as needed. For persistent: Low-dose ICS-formoterol MART. Consider allergen avoidance and flu vaccination.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-40),
                    Source = "Global Initiative for Asthma (GINA)"
                });
            }

            // COVID-19 Management (WHO Guidelines)
            if (conditionLower.Contains("covid") || conditionLower.Contains("coronavirus"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "COVID-19 Management - WHO Guidelines",
                    Condition = "COVID-19",
                    Recommendation = "Mild cases: Supportive care, monitor oxygen saturation. Consider Paxlovid for high-risk patients. Severe cases: Hospitalization, oxygen therapy, consider dexamethasone.",
                    EvidenceLevel = "Grade B",
                    LastUpdated = DateTime.UtcNow.AddDays(-15),
                    Source = "World Health Organization COVID-19 Treatment Guidelines"
                });
            }

            // Antibiotic Stewardship
            if (medicationLower.Contains("antibiotic") || medicationLower.Contains("amoxicillin"))
            {
                guidelines.Add(new ClinicalGuideline
                {
                    Title = "Antibiotic Stewardship Guidelines",
                    Medication = "Antibiotics",
                    Recommendation = "Prescribe antibiotics only when clinically indicated. Use narrow-spectrum agents first. Duration typically 5-7 days for most infections. Avoid antibiotics for viral infections.",
                    EvidenceLevel = "Grade A",
                    LastUpdated = DateTime.UtcNow.AddDays(-25),
                    Source = "WHO AWaRe Classification of Antibiotics"
                });
            }

            return guidelines;
        }

        // Enhanced vital signs reference - in production, this would use clinical guidelines
        // from WHO, American Heart Association, or pediatric clinical references
        private async Task<VitalSignsReference> GetMockVitalSignsReference(int age)
        {
            await Task.Delay(50); // Simulate API call

            var reference = new VitalSignsReference
            {
                Age = age
            };

            // Age-specific vital sign ranges based on clinical guidelines
            if (age < 1) // Neonates (0-11 months)
            {
                reference.BloodPressureSystolicMin = 65;
                reference.BloodPressureSystolicMax = 100;
                reference.BloodPressureDiastolicMin = 45;
                reference.BloodPressureDiastolicMax = 65;
                reference.HeartRateMin = 100;
                reference.HeartRateMax = 160;
                reference.RespiratoryRateMin = 30;
                reference.RespiratoryRateMax = 60;
                reference.TemperatureMin = 36.5m;
                reference.TemperatureMax = 38.0m; // Higher normal range for infants
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else if (age < 3) // Toddlers (1-2 years)
            {
                reference.BloodPressureSystolicMin = 80;
                reference.BloodPressureSystolicMax = 110;
                reference.BloodPressureDiastolicMin = 50;
                reference.BloodPressureDiastolicMax = 70;
                reference.HeartRateMin = 80;
                reference.HeartRateMax = 130;
                reference.RespiratoryRateMin = 20;
                reference.RespiratoryRateMax = 30;
                reference.TemperatureMin = 36.4m;
                reference.TemperatureMax = 37.8m;
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else if (age < 6) // Preschool (3-5 years)
            {
                reference.BloodPressureSystolicMin = 85;
                reference.BloodPressureSystolicMax = 115;
                reference.BloodPressureDiastolicMin = 55;
                reference.BloodPressureDiastolicMax = 75;
                reference.HeartRateMin = 70;
                reference.HeartRateMax = 120;
                reference.RespiratoryRateMin = 18;
                reference.RespiratoryRateMax = 25;
                reference.TemperatureMin = 36.4m;
                reference.TemperatureMax = 37.6m;
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else if (age < 12) // School-age (6-11 years)
            {
                reference.BloodPressureSystolicMin = 90;
                reference.BloodPressureSystolicMax = 120;
                reference.BloodPressureDiastolicMin = 60;
                reference.BloodPressureDiastolicMax = 80;
                reference.HeartRateMin = 60;
                reference.HeartRateMax = 110;
                reference.RespiratoryRateMin = 16;
                reference.RespiratoryRateMax = 22;
                reference.TemperatureMin = 36.3m;
                reference.TemperatureMax = 37.5m;
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else if (age < 18) // Adolescents (12-17 years)
            {
                reference.BloodPressureSystolicMin = 95;
                reference.BloodPressureSystolicMax = 125;
                reference.BloodPressureDiastolicMin = 60;
                reference.BloodPressureDiastolicMax = 82;
                reference.HeartRateMin = 55;
                reference.HeartRateMax = 105;
                reference.RespiratoryRateMin = 14;
                reference.RespiratoryRateMax = 20;
                reference.TemperatureMin = 36.2m;
                reference.TemperatureMax = 37.4m;
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else if (age < 40) // Adults (18-39 years)
            {
                reference.BloodPressureSystolicMin = 90;
                reference.BloodPressureSystolicMax = 120;
                reference.BloodPressureDiastolicMin = 60;
                reference.BloodPressureDiastolicMax = 80;
                reference.HeartRateMin = 50;
                reference.HeartRateMax = 100;
                reference.RespiratoryRateMin = 12;
                reference.RespiratoryRateMax = 20;
                reference.TemperatureMin = 36.1m;
                reference.TemperatureMax = 37.3m;
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else if (age < 65) // Middle-aged adults (40-64 years)
            {
                reference.BloodPressureSystolicMin = 90;
                reference.BloodPressureSystolicMax = 130;
                reference.BloodPressureDiastolicMin = 60;
                reference.BloodPressureDiastolicMax = 85;
                reference.HeartRateMin = 50;
                reference.HeartRateMax = 95;
                reference.RespiratoryRateMin = 12;
                reference.RespiratoryRateMax = 18;
                reference.TemperatureMin = 36.0m;
                reference.TemperatureMax = 37.2m;
                reference.OxygenSaturationMin = 95;
                reference.OxygenSaturationMax = 100;
            }
            else // Elderly (65+ years)
            {
                reference.BloodPressureSystolicMin = 90;
                reference.BloodPressureSystolicMax = 140; // Higher acceptable range for elderly
                reference.BloodPressureDiastolicMin = 60;
                reference.BloodPressureDiastolicMax = 90;
                reference.HeartRateMin = 50;
                reference.HeartRateMax = 90;
                reference.RespiratoryRateMin = 12;
                reference.RespiratoryRateMax = 18;
                reference.TemperatureMin = 35.8m; // Lower normal range in elderly
                reference.TemperatureMax = 37.0m;
                reference.OxygenSaturationMin = 94; // Slightly lower acceptable range
                reference.OxygenSaturationMax = 100;
            }

            // Add clinical notes based on age
            var notes = new List<string>();

            if (age < 1)
            {
                notes.Add("Neonatal vital signs vary significantly with gestational age and birth weight.");
                notes.Add("Axillary temperature may be 0.5-1°C lower than core temperature.");
            }
            else if (age < 5)
            {
                notes.Add("Children have higher heart and respiratory rates than adults.");
                notes.Add("Fever in infants under 3 months requires immediate medical evaluation.");
            }
            else if (age < 18)
            {
                notes.Add("Adolescent vital signs approach adult ranges but may vary with puberty.");
                notes.Add("Blood pressure should be measured with appropriate cuff size.");
            }
            else if (age >= 65)
            {
                notes.Add("Elderly may have lower baseline temperature and reduced fever response.");
                notes.Add("Orthostatic blood pressure measurements recommended for fall risk assessment.");
            }

            reference.ClinicalNotes = string.Join(" ", notes);

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
