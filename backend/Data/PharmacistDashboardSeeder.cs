using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public class PharmacistDashboardSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PharmacistDashboardSeeder> _logger;

        public PharmacistDashboardSeeder(ApplicationDbContext context, ILogger<PharmacistDashboardSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedSampleDataAsync()
        {
            try
            {
                // Check if prescriptions already exist
                var existingPrescriptions = await _context.Prescriptions.AnyAsync();
                if (existingPrescriptions)
                {
                    _logger.LogInformation("Prescriptions already exist, skipping seeding");
                    return;
                }

                // Create sample patients
                var patients = new[]
                {
                    new Patient
                    {
                        Name = "John Banda",
                        IdNumber = "12345678901",
                        PhoneNumber = "+260977123456",
                        Email = "john.banda@email.com",
                        DateOfBirth = new DateTime(1985, 5, 15),
                        Gender = "Male",
                        Address = "Lusaka, Zambia",
                        Allergies = "Penicillin",
                        MedicalHistory = "Hypertension",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Patient
                    {
                        Name = "Mary Phiri",
                        IdNumber = "23456789012",
                        PhoneNumber = "+260977234567",
                        Email = "mary.phiri@email.com",
                        DateOfBirth = new DateTime(1992, 8, 22),
                        Gender = "Female",
                        Address = "Kitwe, Zambia",
                        Allergies = "None",
                        MedicalHistory = "Diabetes Type 2",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Patient
                    {
                        Name = "James Chanda",
                        IdNumber = "34567890123",
                        PhoneNumber = "+260977345678",
                        Email = "james.chanda@email.com",
                        DateOfBirth = new DateTime(1978, 3, 10),
                        Gender = "Male",
                        Address = "Ndola, Zambia",
                        Allergies = "Sulfa drugs",
                        MedicalHistory = "Asthma",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await _context.Patients.AddRangeAsync(patients);
                await _context.SaveChangesAsync();

                // Create sample prescriptions
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                var prescriptions = new[]
                {
                    new Prescription
                    {
                        RxNumber = await GenerateRxNumber(),
                        PatientId = patients[0].Id,
                        PatientName = patients[0].Name,
                        PatientIdNumber = patients[0].IdNumber,
                        DoctorName = "Dr. Sarah Mwale",
                        Medication = "Amoxicillin 500mg",
                        Dosage = "1 tablet twice daily for 7 days",
                        Notes = "Take with food. Patient reports throat pain",
                        TotalCost = 85.50m,
                        Status = "pending",
                        PrescriptionDate = today,
                        IsUrgent = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Prescription
                    {
                        RxNumber = await GenerateRxNumber(),
                        PatientId = patients[1].Id,
                        PatientName = patients[1].Name,
                        PatientIdNumber = patients[1].IdNumber,
                        DoctorName = "Dr. Michael Banda",
                        Medication = "Metformin 500mg",
                        Dosage = "1 tablet twice daily",
                        Notes = "Take after meals. Regular diabetes medication refill",
                        TotalCost = 120.00m,
                        Status = "filled",
                        PrescriptionDate = today,
                        IsUrgent = false,
                        FilledDate = today,
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        UpdatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new Prescription
                    {
                        RxNumber = await GenerateRxNumber(),
                        PatientId = patients[2].Id,
                        PatientName = patients[2].Name,
                        PatientIdNumber = patients[2].IdNumber,
                        DoctorName = "Dr. Grace Phiri",
                        Medication = "Salbutamol Inhaler",
                        Dosage = "2 puffs as needed",
                        Notes = "Use when experiencing shortness of breath. Asthma exacerbation",
                        TotalCost = 250.00m,
                        Status = "pending",
                        PrescriptionDate = yesterday,
                        IsUrgent = true,
                        CreatedAt = DateTime.UtcNow.AddHours(-4),
                        UpdatedAt = DateTime.UtcNow.AddHours(-4)
                    },
                    new Prescription
                    {
                        RxNumber = await GenerateRxNumber(),
                        PatientId = patients[0].Id,
                        PatientName = patients[0].Name,
                        PatientIdNumber = patients[0].IdNumber,
                        DoctorName = "Dr. Sarah Mwale",
                        Medication = "Lisinopril 10mg",
                        Dosage = "1 tablet daily",
                        Notes = "Take in the morning. Blood pressure medication",
                        TotalCost = 95.00m,
                        Status = "approved",
                        PrescriptionDate = yesterday,
                        IsUrgent = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new Prescription
                    {
                        RxNumber = await GenerateRxNumber(),
                        PatientId = patients[1].Id,
                        PatientName = patients[1].Name,
                        PatientIdNumber = patients[1].IdNumber,
                        DoctorName = "Dr. Michael Banda",
                        Medication = "Insulin Glargine",
                        Dosage = "10 units at bedtime",
                        Notes = "Subcutaneous injection. Long-acting insulin",
                        TotalCost = 450.00m,
                        Status = "pending",
                        PrescriptionDate = today,
                        IsUrgent = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        UpdatedAt = DateTime.UtcNow.AddHours(-1)
                    }
                };

                await _context.Prescriptions.AddRangeAsync(prescriptions);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} sample prescriptions and patients", prescriptions.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding pharmacist dashboard data");
                throw;
            }
        }

        private async Task<string> GenerateRxNumber()
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
    }
}
