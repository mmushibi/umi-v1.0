using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseTestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public DatabaseTestController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                // Test basic database connection
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    return Ok(new { success = false, message = "Cannot connect to database" });
                }
                
                // Test entity counts
                var results = new
                {
                    success = true,
                    message = "Database connection successful",
                    entities = new
                    {
                        patients = await _context.Patients.CountAsync(),
                        prescriptions = await _context.Prescriptions.CountAsync(),
                        inventoryItems = await _context.InventoryItems.CountAsync(),
                        suppliers = await _context.Suppliers.CountAsync(),
                        clinicalNotes = await _context.ClinicalNotes.CountAsync(),
                        shifts = await _context.Shifts.CountAsync(),
                        reportSchedules = await _context.ReportSchedules.CountAsync(),
                        pharmacistProfiles = await _context.PharmacistProfiles.CountAsync()
                    }
                };
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, error = ex.StackTrace });
            }
        }
        
        [HttpGet("test-relationships")]
        public async Task<IActionResult> TestEntityRelationships()
        {
            try
            {
                // Test Patient-Prescription relationship
                var patientWithPrescriptions = await _context.Patients
                    .Include(p => p.Prescriptions)
                    .FirstOrDefaultAsync();
                
                // Test Prescription-PrescriptionItem relationship
                var prescriptionWithItems = await _context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync();
                
                // Test ClinicalNote relationships
                var clinicalNoteWithRelations = await _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Tenant)
                    .FirstOrDefaultAsync();
                
                return Ok(new
                {
                    success = true,
                    message = "Entity relationships test successful",
                    data = new
                    {
                        patientPrescriptions = patientWithPrescriptions?.Prescriptions?.Count ?? 0,
                        prescriptionItems = prescriptionWithItems?.PrescriptionItems?.Count ?? 0,
                        clinicalNotePatient = clinicalNoteWithRelations?.Patient != null,
                        clinicalNoteTenant = clinicalNoteWithRelations?.Tenant != null
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }
        
        [HttpPost("test-crud")]
        public async Task<IActionResult> TestCrudOperations()
        {
            try
            {
                // Test Create - Clinical Note
                var clinicalNote = new ClinicalNote
                {
                    PatientId = 1, // Assuming patient with ID 1 exists
                    NoteType = "Test Note",
                    Content = "This is a test clinical note created via API",
                    Diagnosis = "Test Diagnosis",
                    Symptoms = "Test Symptoms",
                    Treatment = "Test Treatment",
                    FollowUpRequired = false,
                    TenantId = "TEST01",
                    CreatedBy = "test-user"
                };
                
                _context.ClinicalNotes.Add(clinicalNote);
                await _context.SaveChangesAsync();
                
                // Test Read
                var createdNote = await _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .FirstOrDefaultAsync(cn => cn.Id == clinicalNote.Id);
                
                // Test Update
                createdNote.Content = "Updated test content";
                _context.ClinicalNotes.Update(createdNote);
                await _context.SaveChangesAsync();
                
                // Test Delete
                _context.ClinicalNotes.Remove(createdNote);
                await _context.SaveChangesAsync();
                
                return Ok(new
                {
                    success = true,
                    message = "CRUD operations test successful",
                    createdId = clinicalNote.Id,
                    updatedContent = "Updated test content",
                    deleted = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, error = ex.StackTrace });
            }
        }
    }
}
