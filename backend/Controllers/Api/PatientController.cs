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
    public class PatientController : ControllerBase
    {
        private readonly ILogger<PatientController> _logger;
        private readonly ApplicationDbContext _context;

        public PatientController(
            ILogger<PatientController> logger,
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var query = _context.Patients
                    .AsNoTracking()
                    .Where(p => p.TenantId == tenantId && p.IsActive);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.Name.Contains(search) ||
                        (p.Email != null && p.Email.Contains(search)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(search)) ||
                        (p.IdNumber != null && p.IdNumber.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    // Since Patient entity doesn't have Status field, we'll filter by IsActive
                    if (status.ToLower() == "active")
                        query = query.Where(p => p.IsActive);
                    else if (status.ToLower() == "inactive")
                        query = query.Where(p => !p.IsActive);
                }

                var totalCount = await query.CountAsync();
                var patients = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    patients,
                    totalCount,
                    currentPage = page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.IsActive);

                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] CreatePatientRequest request)
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

                // Check for duplicate email or ID number
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        (p.Email == request.Email && p.TenantId == tenantId) ||
                        (p.IdNumber == request.IdNumber && p.TenantId == tenantId && !string.IsNullOrEmpty(request.IdNumber)));

                if (existingPatient != null)
                {
                    return Conflict(new { error = "Patient with this email or ID number already exists" });
                }

                var patient = new Patient
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Phone = request.Phone,
                    Address = request.Address,
                    IdNumber = request.IdNumber,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    MedicalHistory = request.MedicalHistory,
                    Allergies = request.Allergies,
                    TenantId = tenantId,
                    BranchId = request.BranchId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Patient {PatientId} created by user {UserId}", patient.Id, userId);

                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Patient>> UpdatePatient(int id, [FromBody] UpdatePatientRequest request)
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

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.IsActive);

                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                // Check for duplicate email or ID number (excluding current patient)
                var duplicatePatient = await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id != id &&
                        p.TenantId == tenantId &&
                        ((p.Email == request.Email) ||
                         (p.IdNumber == request.IdNumber && !string.IsNullOrEmpty(request.IdNumber))));

                if (duplicatePatient != null)
                {
                    return Conflict(new { error = "Patient with this email or ID number already exists" });
                }

                patient.Name = request.Name;
                patient.Email = request.Email;
                patient.PhoneNumber = request.PhoneNumber;
                patient.Phone = request.Phone;
                patient.Address = request.Address;
                patient.IdNumber = request.IdNumber;
                patient.Gender = request.Gender;
                patient.DateOfBirth = request.DateOfBirth;
                patient.MedicalHistory = request.MedicalHistory;
                patient.Allergies = request.Allergies;
                patient.BranchId = request.BranchId;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Patient {PatientId} updated by user {UserId}", patient.Id, userId);

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePatient(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.IsActive);

                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                // Check if patient has active prescriptions
                var activePrescriptions = await _context.Prescriptions
                    .AnyAsync(p => p.PatientId == id && p.Status != "Completed");

                if (activePrescriptions)
                {
                    return BadRequest(new { error = "Cannot delete patient with active prescriptions" });
                }

                patient.IsActive = false;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Patient {PatientId} deleted by user {UserId}", id, userId);

                return Ok(new { message = "Patient deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Patient>>> SearchPatients([FromQuery] string query)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest(new { error = "Search query must be at least 2 characters" });
                }

                var patients = await _context.Patients
                    .AsNoTracking()
                    .Where(p =>
                        p.TenantId == tenantId &&
                        p.IsActive &&
                        (p.Name.Contains(query) ||
                         (p.Email != null && p.Email.Contains(query)) ||
                         (p.PhoneNumber != null && p.PhoneNumber.Contains(query)) ||
                         (p.IdNumber != null && p.IdNumber.Contains(query))))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with query: {Query}", query);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}/prescriptions")]
        public async Task<ActionResult<IEnumerable<Prescription>>> GetPatientPrescriptions(int id)
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
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.IsActive);

                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                var prescriptions = await _context.Prescriptions
                    .AsNoTracking()
                    .Where(p => p.PatientId == id && p.TenantId == tenantId)
                    .Include(p => p.PrescriptionItems)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return Ok(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescriptions for patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
