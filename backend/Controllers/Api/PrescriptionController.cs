using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using UmiHealthPOS.DTOs;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrescriptionController : ControllerBase
    {
        private readonly ILogger<PrescriptionController> _logger;
        private readonly ApplicationDbContext _context;

        public PrescriptionController(
            ILogger<PrescriptionController> logger,
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
        public async Task<ActionResult<IEnumerable<Prescription>>> GetPrescriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int? patientId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var query = _context.Prescriptions
                    .AsNoTracking()
                    .Where(p => p.TenantId == tenantId);

                query = query.Include(p => p.Patient);
                query = query.Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Product);
                query = query.Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.InventoryItem);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.PrescriptionNumber.Contains(search) ||
                        (p.PatientName != null && p.PatientName.Contains(search)) ||
                        (p.Patient != null && p.Patient.Name.Contains(search)) ||
                        (p.DoctorName != null && p.DoctorName.Contains(search)) ||
                        (p.Medication != null && p.Medication.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (patientId.HasValue)
                {
                    query = query.Where(p => p.PatientId == patientId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.PrescriptionDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.PrescriptionDate <= endDate.Value);
                }

                var totalCount = await query.CountAsync();
                var prescriptions = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    prescriptions,
                    totalCount,
                    currentPage = page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescriptions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Prescription>> GetPrescription(int id)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "Tenant not found" });
                }

                var prescription = await _context.Prescriptions
                    .AsNoTracking()
                    .Where(p => p.Id == id && p.TenantId == tenantId)
                    .Include(p => p.Patient)
                    .Include(p => p.PrescriptionItems)
                        .ThenInclude(pi => pi.Product)
                    .Include(p => p.PrescriptionItems)
                        .ThenInclude(pi => pi.InventoryItem)
                    .FirstOrDefaultAsync();

                if (prescription == null)
                {
                    return NotFound(new { error = "Prescription not found" });
                }

                return Ok(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescription {PrescriptionId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Prescription>> CreatePrescription([FromBody] CreatePrescriptionRequest request)
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

                // Generate prescription number
                var prescriptionNumber = await GeneratePrescriptionNumber(tenantId);

                var prescription = new Prescription
                {
                    PrescriptionNumber = prescriptionNumber,
                    PatientId = request.PatientId,
                    PatientName = patient.Name,
                    PatientIdNumber = patient.IdNumber,
                    DoctorName = request.DoctorName,
                    DoctorRegistrationNumber = request.DoctorRegistrationNumber,
                    Notes = request.Notes,
                    Medication = request.Medication,
                    Dosage = request.Dosage,
                    Instructions = request.Instructions,
                    PrescriptionDate = request.PrescriptionDate ?? DateTime.UtcNow,
                    FilledDate = request.FilledDate,
                    ExpiryDate = request.ExpiryDate,
                    TotalCost = request.TotalCost,
                    IsUrgent = request.IsUrgent ?? false,
                    RxNumber = request.RxNumber,
                    TenantId = tenantId,
                    BranchId = request.BranchId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                // Add prescription items if provided
                if (request.PrescriptionItems != null && request.PrescriptionItems.Any())
                {
                    foreach (var itemRequest in request.PrescriptionItems)
                    {
                        var prescriptionItem = new PrescriptionItem
                        {
                            PrescriptionId = prescription.Id,
                            ProductId = itemRequest.ProductId,
                            InventoryItemId = itemRequest.InventoryItemId,
                            Quantity = itemRequest.Quantity,
                            UnitPrice = itemRequest.UnitPrice,
                            TotalPrice = itemRequest.TotalPrice,
                            MedicationName = itemRequest.MedicationName,
                            Dosage = itemRequest.Dosage,
                            Instructions = itemRequest.Instructions,
                            ExpiryDate = itemRequest.ExpiryDate,
                            Duration = itemRequest.Duration
                        };

                        _context.PrescriptionItems.Add(prescriptionItem);
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Prescription {PrescriptionId} created by user {UserId}", prescription.Id, userId);

                return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Prescription>> UpdatePrescription(int id, [FromBody] UpdatePrescriptionRequest request)
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

                var prescription = await _context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

                if (prescription == null)
                {
                    return NotFound(new { error = "Prescription not found" });
                }

                // Update prescription details
                prescription.DoctorName = request.DoctorName;
                prescription.DoctorRegistrationNumber = request.DoctorRegistrationNumber;
                prescription.Notes = request.Notes;
                prescription.Medication = request.Medication;
                prescription.Dosage = request.Dosage;
                prescription.Instructions = request.Instructions;
                prescription.PrescriptionDate = request.PrescriptionDate;
                prescription.FilledDate = request.FilledDate;
                prescription.ExpiryDate = request.ExpiryDate;
                prescription.TotalCost = request.TotalCost;
                prescription.IsUrgent = request.IsUrgent ?? prescription.IsUrgent;
                prescription.RxNumber = request.RxNumber;
                prescription.Status = request.Status ?? prescription.Status;
                prescription.UpdatedAt = DateTime.UtcNow;

                // Update patient info if changed
                if (request.PatientId.HasValue && request.PatientId.Value != prescription.PatientId)
                {
                    var patient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.Id == request.PatientId.Value && p.TenantId == tenantId && p.IsActive);

                    if (patient == null)
                    {
                        return BadRequest(new { error = "Patient not found" });
                    }

                    prescription.PatientId = request.PatientId.Value;
                    prescription.PatientName = patient.Name;
                    prescription.PatientIdNumber = patient.IdNumber ?? string.Empty;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Prescription {PrescriptionId} updated by user {UserId}", prescription.Id, userId);

                return Ok(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prescription {PrescriptionId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ... (rest of the code remains the same)

        private async Task<string> GeneratePrescriptionNumber(string tenantId)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Prescriptions
                .CountAsync(p => p.TenantId == tenantId && p.PrescriptionNumber != null && p.PrescriptionNumber.StartsWith(datePrefix));

            return $"RX{datePrefix}{(count + 1):D4}";
        }
    }
}
