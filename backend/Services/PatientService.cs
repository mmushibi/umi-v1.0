using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Hubs;

namespace UmiHealthPOS.Services
{
    public interface IPatientService
    {
        Task<List<Patient>> GetPatientsAsync();
        Task<Patient?> GetPatientAsync(int id);
        Task<Patient> CreatePatientAsync(CreatePatientRequest request);
        Task<Patient?> UpdatePatientAsync(int id, CreatePatientRequest request);
        Task<bool> DeletePatientAsync(int id);
        Task<byte[]> ExportPatientsToCsvAsync();
        Task<CsvImportResult> ImportPatientsFromCsvAsync(IFormFile file);
        Task<List<Patient>> SearchPatientsAsync(string query);
    }

    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PatientHub> _hubContext;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            ApplicationDbContext context,
            IHubContext<PatientHub> hubContext,
            ILogger<PatientService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<List<Patient>> GetPatientsAsync()
        {
            try
            {
                var patients = await _context.Patients
                    .Include(p => p.Prescriptions)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} patients", patients.Count);
                return patients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                throw;
            }
        }

        public async Task<Patient?> GetPatientAsync(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.Prescriptions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient != null)
                {
                    _logger.LogInformation("Retrieved patient with ID: {Id}", id);
                }
                else
                {
                    _logger.LogWarning("Patient with ID: {Id} not found", id);
                }

                return patient;
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
                // Validate unique constraints
                if (!string.IsNullOrEmpty(request.IdNumber))
                {
                    var existingPatient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.IdNumber == request.IdNumber);

                    if (existingPatient != null)
                    {
                        throw new InvalidOperationException("A patient with this ID number already exists.");
                    }
                }

                var patient = new Patient
                {
                    Name = request.Name?.Trim() ?? throw new ArgumentException("Name is required"),
                    IdNumber = request.IdNumber?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Phone = request.PhoneNumber?.Trim(), // For backward compatibility
                    Email = request.Email?.Trim()?.ToLower(),
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    Address = request.Address?.Trim(),
                    MedicalHistory = request.MedicalHistory?.Trim(),
                    Allergies = request.Allergies?.Trim(),
                    IsActive = true,
                    TenantId = GetCurrentTenantId(),
                    BranchId = GetCurrentBranchId(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new patient with ID: {Id}, Name: {Name}", patient.Id, patient.Name);

                // Real-time notification
                await _hubContext.Clients.All.SendAsync("PatientCreated", new
                {
                    patient.Id,
                    patient.Name,
                    patient.PhoneNumber,
                    patient.Email,
                    patient.CreatedAt
                });

                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                throw;
            }
        }

        public async Task<Patient?> UpdatePatientAsync(int id, CreatePatientRequest request)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    _logger.LogWarning("Patient with ID: {Id} not found for update", id);
                    return null;
                }

                // Validate unique constraints if ID number is being changed
                if (!string.IsNullOrEmpty(request.IdNumber) && request.IdNumber != patient.IdNumber)
                {
                    var existingPatient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.IdNumber == request.IdNumber && p.Id != id);

                    if (existingPatient != null)
                    {
                        throw new InvalidOperationException("A patient with this ID number already exists.");
                    }
                }

                // Update patient properties
                patient.Name = request.Name?.Trim() ?? patient.Name;
                patient.IdNumber = request.IdNumber?.Trim() ?? patient.IdNumber;
                patient.PhoneNumber = request.PhoneNumber?.Trim() ?? patient.PhoneNumber;
                patient.Phone = patient.PhoneNumber; // Keep in sync
                patient.Email = request.Email?.Trim()?.ToLower() ?? patient.Email;
                patient.Gender = request.Gender ?? patient.Gender;
                patient.DateOfBirth = request.DateOfBirth ?? patient.DateOfBirth;
                patient.Address = request.Address?.Trim() ?? patient.Address;
                patient.MedicalHistory = request.MedicalHistory?.Trim() ?? patient.MedicalHistory;
                patient.Allergies = request.Allergies?.Trim() ?? patient.Allergies;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated patient with ID: {Id}, Name: {Name}", patient.Id, patient.Name);

                // Real-time notification
                await _hubContext.Clients.All.SendAsync("PatientUpdated", new
                {
                    patient.Id,
                    patient.Name,
                    patient.PhoneNumber,
                    patient.Email,
                    patient.UpdatedAt
                });

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
                    _logger.LogWarning("Patient with ID: {Id} not found for deletion", id);
                    return false;
                }

                // Check if patient has prescriptions
                var hasPrescriptions = await _context.Prescriptions
                    .AnyAsync(p => p.PatientId == id);

                if (hasPrescriptions)
                {
                    // Soft delete - mark as inactive instead of deleting
                    patient.IsActive = false;
                    patient.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Soft deleted patient with ID: {Id} (has prescriptions)", id);

                    // Real-time notification
                    await _hubContext.Clients.All.SendAsync("PatientDeactivated", new
                    {
                        patient.Id,
                        patient.Name,
                        patient.UpdatedAt
                    });
                }
                else
                {
                    // Hard delete - safe to remove
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Hard deleted patient with ID: {Id}", id);

                    // Real-time notification
                    await _hubContext.Clients.All.SendAsync("PatientDeleted", new
                    {
                        patient.Id,
                        patient.Name
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {Id}", id);
                throw;
            }
        }

        public async Task<byte[]> ExportPatientsToCsvAsync()
        {
            try
            {
                var patients = await _context.Patients
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("ID,Name,ID Number,Phone,Email,Gender,Date of Birth,Address,Allergies,Medical History,Status,Created At");

                foreach (var patient in patients)
                {
                    csv.AppendLine($"{patient.Id}," +
                        $"\"{EscapeCsv(patient.Name)}\"," +
                        $"\"{EscapeCsv(patient.IdNumber ?? "")}\"," +
                        $"\"{EscapeCsv(patient.PhoneNumber ?? "")}\"," +
                        $"\"{EscapeCsv(patient.Email ?? "")}\"," +
                        $"\"{EscapeCsv(patient.Gender ?? "")}\"," +
                        $"\"{patient.DateOfBirth:yyyy-MM-dd}\"," +
                        $"\"{EscapeCsv(patient.Address ?? "")}\"," +
                        $"\"{EscapeCsv(patient.Allergies ?? "")}\"," +
                        $"\"{EscapeCsv(patient.MedicalHistory ?? "")}\"," +
                        $"\"{(patient.IsActive ? "Active" : "Inactive")}\"," +
                        $"\"{patient.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }

                _logger.LogInformation("Exported {Count} patients to CSV", patients.Count);
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting patients to CSV");
                throw;
            }
        }

        public async Task<CsvImportResult> ImportPatientsFromCsvAsync(IFormFile file)
        {
            var result = new CsvImportResult();

            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("No file provided");
                }

                using var reader = new StreamReader(file.OpenReadStream());
                var csvContent = await reader.ReadToEndAsync();
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length <= 1) // Only header row
                {
                    result.Errors.Add("CSV file contains no data rows");
                    return result;
                }

                // Skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Length < 11) // Minimum required columns
                        {
                            result.Errors.Add($"Row {i + 1}: Insufficient columns");
                            continue;
                        }

                        var patient = new Patient
                        {
                            Name = values[1]?.Trim() ?? throw new InvalidOperationException("Name is required"),
                            IdNumber = values[2]?.Trim(),
                            PhoneNumber = values[3]?.Trim(),
                            Phone = values[3]?.Trim(), // Keep in sync
                            Email = values[4]?.Trim()?.ToLower(),
                            Gender = values[5]?.Trim(),
                            DateOfBirth = ParseDateTime(values[6]),
                            Address = values[7]?.Trim(),
                            Allergies = values[8]?.Trim(),
                            MedicalHistory = values[9]?.Trim(),
                            IsActive = (values[10]?.Trim().Equals("Active", StringComparison.OrdinalIgnoreCase) ?? false),
                            TenantId = GetCurrentTenantId(),
                            BranchId = GetCurrentBranchId(),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // Check for duplicates
                        if (!string.IsNullOrEmpty(patient.IdNumber))
                        {
                            var existing = await _context.Patients
                                .FirstOrDefaultAsync(p => p.IdNumber == patient.IdNumber);

                            if (existing != null)
                            {
                                result.Errors.Add($"Row {i + 1}: Patient with ID number '{patient.IdNumber}' already exists");
                                continue;
                            }
                        }

                        _context.Patients.Add(patient);
                        result.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                if (result.ImportedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully imported {Count} patients", result.ImportedCount);

                    // Real-time notification for bulk import
                    await _hubContext.Clients.All.SendAsync("PatientsBulkImported", new
                    {
                        Count = result.ImportedCount,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing patients from CSV");
                result.Errors.Add($"General error: {ex.Message}");
            }

            return result;
        }

        public async Task<List<Patient>> SearchPatientsAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return await GetPatientsAsync();
                }

                var normalizedQuery = query.ToLower().Trim();

                var patients = await _context.Patients
                    .Include(p => p.Prescriptions)
                    .Where(p =>
                        p.Name.ToLower().Contains(normalizedQuery) ||
                        (p.IdNumber != null && p.IdNumber.ToLower().Contains(normalizedQuery)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(normalizedQuery)) ||
                        (p.Email != null && p.Email.ToLower().Contains(normalizedQuery)))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Search for '{Query}' returned {Count} patients", query, patients.Count);
                return patients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with query: {Query}", query);
                throw;
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\"", "\"\"");
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        private DateTime? ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, out var result))
                return result;

            return null;
        }

        private string GetCurrentTenantId()
        {
            // TODO: Get from current user context
            return "TEN001";
        }

        private int? GetCurrentBranchId()
        {
            // TODO: Get from current user context
            return 1;
        }
    }

}
