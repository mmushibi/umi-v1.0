using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.IO;

namespace UmiHealthPOS.Services
{
    public interface IPrescriptionService
    {
        Task<List<Prescription>> GetPrescriptionsAsync();
        Task<Prescription> GetPrescriptionAsync(int id);
        Task<Prescription> CreatePrescriptionAsync(CreatePrescriptionRequest request);
        Task<Prescription> UpdatePrescriptionAsync(int id, UpdatePrescriptionRequest request);
        Task<bool> DeletePrescriptionAsync(int id);
        Task<bool> FillPrescriptionAsync(int id);
        Task<List<Patient>> GetPatientsAsync();
        Task<Patient> GetPatientAsync(int id);
        Task<Patient> CreatePatientAsync(CreatePatientRequest request);
        Task<Patient> UpdatePatientAsync(int id, CreatePatientRequest request);
        Task<bool> DeletePatientAsync(int id);
        Task<CsvImportResult> ImportPatientsFromCsvAsync(IFormFile file);
        Task<byte[]> ExportPatientsToCsvAsync();
        Task<byte[]> ExportPrescriptionsToCsvAsync();
        Task<string> GenerateRxNumberAsync();
    }

    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(ApplicationDbContext context, ILogger<PrescriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Prescription>> GetPrescriptionsAsync()
        {
            try
            {
                return await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.InventoryItem)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescriptions");
                throw;
            }
        }

        public async Task<Prescription> GetPrescriptionAsync(int id)
        {
            try
            {
                return await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.InventoryItem)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<Prescription> CreatePrescriptionAsync(CreatePrescriptionRequest request)
        {
            try
            {
                var prescription = new Prescription
                {
                    RxNumber = await GenerateRxNumberAsync(),
                    PatientId = request.PatientId,
                    PatientName = request.PatientName,
                    PatientIdNumber = request.PatientIdNumber,
                    DoctorName = request.DoctorName,
                    DoctorRegistrationNumber = request.DoctorRegistrationNumber,
                    Medication = request.Medication,
                    Dosage = request.Dosage,
                    Instructions = request.Instructions,
                    TotalCost = request.TotalCost,
                    Status = "pending",
                    PrescriptionDate = request.PrescriptionDate,
                    ExpiryDate = request.ExpiryDate,
                    Notes = request.Notes,
                    IsUrgent = request.IsUrgent
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
                            InventoryItemId = itemRequest.InventoryItemId,
                            MedicationName = itemRequest.MedicationName,
                            Dosage = itemRequest.Dosage,
                            Quantity = itemRequest.Quantity,
                            Instructions = itemRequest.Instructions,
                            UnitPrice = itemRequest.UnitPrice,
                            TotalPrice = itemRequest.TotalPrice
                        };
                        _context.PrescriptionItems.Add(prescriptionItem);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created new prescription with RX number: {RxNumber}", prescription.RxNumber);
                return prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription");
                throw;
            }
        }

        public async Task<Prescription> UpdatePrescriptionAsync(int id, UpdatePrescriptionRequest request)
        {
            try
            {
                var prescription = await _context.Prescriptions.FindAsync(id);
                if (prescription == null)
                    return null;

                prescription.PatientName = request.PatientName;
                prescription.DoctorName = request.DoctorName;
                prescription.Medication = request.Medication;
                prescription.Dosage = request.Dosage;
                prescription.Instructions = request.Instructions;
                prescription.TotalCost = request.TotalCost;
                prescription.Notes = request.Notes;
                prescription.IsUrgent = request.IsUrgent;
                prescription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated prescription with ID: {Id}", id);
                return prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prescription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeletePrescriptionAsync(int id)
        {
            try
            {
                var prescription = await _context.Prescriptions.FindAsync(id);
                if (prescription == null)
                    return false;

                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted prescription with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prescription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> FillPrescriptionAsync(int id)
        {
            try
            {
                var prescription = await _context.Prescriptions.FindAsync(id);
                if (prescription == null)
                    return false;

                prescription.Status = "filled";
                prescription.FilledDate = DateTime.Today;
                prescription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Filled prescription with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filling prescription with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<Patient>> GetPatientsAsync()
        {
            try
            {
                return await _context.Patients
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                throw;
            }
        }

        public async Task<Patient> GetPatientAsync(int id)
        {
            try
            {
                return await _context.Patients.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with ID: {Id}", id);
                throw;
            }
        }

        public async Task<Patient> CreatePatientAsync(CreatePatientRequest request)
        {
            try
            {
                var patient = new Patient
                {
                    Name = request.Name,
                    IdNumber = request.IdNumber,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Address = request.Address,
                    Allergies = request.Allergies,
                    MedicalHistory = request.MedicalHistory,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new patient: {Name}", patient.Name);
                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                throw;
            }
        }

        public async Task<Patient> UpdatePatientAsync(int id, CreatePatientRequest request)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return null;
                }

                patient.Name = request.Name;
                patient.IdNumber = request.IdNumber;
                patient.PhoneNumber = request.PhoneNumber;
                patient.Email = request.Email;
                patient.DateOfBirth = request.DateOfBirth;
                patient.Gender = request.Gender;
                patient.Address = request.Address;
                patient.Allergies = request.Allergies;
                patient.MedicalHistory = request.MedicalHistory;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated patient: {Name}", patient.Name);
                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return false;
                }

                // Soft delete by setting IsActive to false
                patient.IsActive = false;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted patient: {Name}", patient.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {Id}", id);
                throw;
            }
        }

        public async Task<CsvImportResult> ImportPatientsFromCsvAsync(IFormFile file)
        {
            var result = new CsvImportResult();

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(headerLine))
                {
                    result.Errors.Add("CSV file is empty");
                    return result;
                }

                var lineCount = 1;
                while (!reader.EndOfStream)
                {
                    lineCount++;
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = line.Split(',');
                    if (values.Length < 8)
                    {
                        result.Errors.Add($"Line {lineCount}: Insufficient columns");
                        continue;
                    }

                    try
                    {
                        var patientRequest = new CreatePatientRequest
                        {
                            Name = values[0].Trim('"'),
                            IdNumber = values[1].Trim('"'),
                            PhoneNumber = values[2].Trim('"'),
                            Email = values[3].Trim('"'),
                            Gender = values[4].Trim('"'),
                            Address = values[5].Trim('"'),
                            Allergies = values[6].Trim('"'),
                            MedicalHistory = values[7].Trim('"')
                        };

                        if (DateTime.TryParse(values[8].Trim('"'), out var dateOfBirth))
                        {
                            patientRequest.DateOfBirth = dateOfBirth;
                        }

                        await CreatePatientAsync(patientRequest);
                        result.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Line {lineCount}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Imported {Count} patients from CSV", result.ImportedCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing patients from CSV");
                result.Errors.Add($"General error: {ex.Message}");
                return result;
            }
        }

        public async Task<byte[]> ExportPatientsToCsvAsync()
        {
            try
            {
                var patients = await GetPatientsAsync();
                var csv = new StringBuilder();

                // Header
                csv.AppendLine("Name,ID Number,Phone,Email,Gender,Address,Allergies,Medical History,Date of Birth,Registration Date,Status");

                // Data rows
                foreach (var patient in patients)
                {
                    csv.AppendLine($"\"{patient.Name}\"," +
                                  $"\"{patient.IdNumber}\"," +
                                  $"\"{patient.PhoneNumber}\"," +
                                  $"\"{patient.Email}\"," +
                                  $"\"{patient.Gender}\"," +
                                  $"\"{patient.Address}\"," +
                                  $"\"{patient.Allergies}\"," +
                                  $"\"{patient.MedicalHistory}\"," +
                                  $"{(patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "")}," +
                                  $"{patient.CreatedAt:yyyy-MM-dd}," +
                                  $"{(patient.IsActive ? "Active" : "Inactive")}");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting patients to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportPrescriptionsToCsvAsync()
        {
            try
            {
                var prescriptions = await GetPrescriptionsAsync();
                var csv = new StringBuilder();

                // Header
                csv.AppendLine("RX Number,Patient Name,Patient ID,Doctor Name,Medication,Dosage,Status,Prescription Date,Filled Date,Total Cost,Is Urgent");

                // Data rows
                foreach (var prescription in prescriptions)
                {
                    csv.AppendLine($"{prescription.RxNumber}," +
                                  $"\"{prescription.PatientName}\"," +
                                  $"\"{prescription.PatientIdNumber}\"," +
                                  $"\"{prescription.DoctorName}\"," +
                                  $"\"{prescription.Medication}\"," +
                                  $"\"{prescription.Dosage}\"," +
                                  $"{prescription.Status}," +
                                  $"{prescription.PrescriptionDate:yyyy-MM-dd}," +
                                  $"{(prescription.FilledDate?.ToString("yyyy-MM-dd") ?? "")}," +
                                  $"ZMW{prescription.TotalCost:F2}," +
                                  $"{prescription.IsUrgent}");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting prescriptions to CSV");
                throw;
            }
        }

        public async Task<string> GenerateRxNumberAsync()
        {
            try
            {
                var datePrefix = DateTime.Now.ToString("yyyyMMdd");
                var lastPrescription = await _context.Prescriptions
                    .Where(p => p.RxNumber.StartsWith(datePrefix))
                    .OrderByDescending(p => p.RxNumber)
                    .FirstOrDefaultAsync();

                int sequence = 1;
                if (lastPrescription != null)
                {
                    var lastSequence = lastPrescription.RxNumber.Substring(datePrefix.Length);
                    if (int.TryParse(lastSequence, out int lastSeq))
                    {
                        sequence = lastSeq + 1;
                    }
                }

                return $"{datePrefix}{sequence:D4}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating RX number");
                throw;
            }
        }
    }

    // Request/Response Models
    public class CreatePrescriptionRequest
    {
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? PatientIdNumber { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorRegistrationNumber { get; set; }
        public string? Medication { get; set; }
        public string? Dosage { get; set; }
        public string? Instructions { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public bool IsUrgent { get; set; }
        public List<CreatePrescriptionItemRequest>? PrescriptionItems { get; set; }
    }

    public class UpdatePrescriptionRequest
    {
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? Medication { get; set; }
        public string? Dosage { get; set; }
        public string? Instructions { get; set; }
        public decimal TotalCost { get; set; }
        public string? Notes { get; set; }
        public bool IsUrgent { get; set; }
    }

    public class CreatePrescriptionItemRequest
    {
        public int InventoryItemId { get; set; }
        public string? MedicationName { get; set; }
        public string? Dosage { get; set; }
        public int Quantity { get; set; }
        public string? Instructions { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
