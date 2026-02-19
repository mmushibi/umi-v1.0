using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.DTOs
{
    // Inventory Item Request DTOs
    public class CreateInventoryItemRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string GenericName { get; set; } = string.Empty;
        
        public string BrandName { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public string BatchNumber { get; set; } = string.Empty;
        
        public DateTime ManufactureDate { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        
        public string LicenseNumber { get; set; } = string.Empty;
        
        public string ZambiaRegNumber { get; set; } = string.Empty;
        
        [Required]
        public string PackingType { get; set; } = string.Empty;
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal SellingPrice { get; set; }
        
        public int ReorderLevel { get; set; }
        
        public string Supplier { get; set; } = string.Empty;
        
        public string StorageConditions { get; set; } = string.Empty;
        
        public bool RequiresPrescription { get; set; }
        
        public bool IsControlledSubstance { get; set; }
    }

    public class UpdateInventoryItemRequest
    {
        public string Name { get; set; } = string.Empty;
        
        public string GenericName { get; set; } = string.Empty;
        
        public string BrandName { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        public string BatchNumber { get; set; } = string.Empty;
        
        public DateTime? ManufactureDate { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        public string LicenseNumber { get; set; } = string.Empty;
        
        public string ZambiaRegNumber { get; set; } = string.Empty;
        
        public string PackingType { get; set; } = string.Empty;
        
        public int? Quantity { get; set; }
        
        public decimal? UnitPrice { get; set; }
        
        public decimal? SellingPrice { get; set; }
        
        public int? ReorderLevel { get; set; }
        
        public string Supplier { get; set; } = string.Empty;
        
        public string StorageConditions { get; set; } = string.Empty;
        
        public bool? RequiresPrescription { get; set; }
        
        public bool? IsControlledSubstance { get; set; }
    }

    // Prescription Request DTOs
    public class CreatePrescriptionRequest
    {
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int DoctorId { get; set; }
        
        [Required]
        public string Diagnosis { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        
        [Required]
        public List<PrescriptionItemRequest> Items { get; set; } = new();
        
        // Additional properties for prescription creation
        [Required]
        [StringLength(200)]
        public string DoctorName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DoctorRegistrationNumber { get; set; }

        [StringLength(500)]
        public string? Medication { get; set; }

        [StringLength(100)]
        public string? Dosage { get; set; }

        [StringLength(200)]
        public string? Instructions { get; set; }

        public DateTime? PrescriptionDate { get; set; }
        public DateTime? FilledDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? TotalCost { get; set; }
        public bool? IsUrgent { get; set; }
        public string RxNumber { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        
        // Computed properties
        public List<PrescriptionItemRequest> PrescriptionItems => Items;
    }

    public class UpdatePrescriptionRequest
    {
        public int? PatientId { get; set; }
        
        public int? DoctorId { get; set; }
        
        public string Diagnosis { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        
        public List<PrescriptionItemRequest>? Items { get; set; }
        
        public string? DoctorRegistrationNumber { get; set; }
        
        public DateTime? PrescriptionDate { get; set; }
        
        public DateTime? FilledDate { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        public string? RxNumber { get; set; }
        
        public string? Status { get; set; }
        
        // Additional properties for prescription updates
        public string? PatientName { get; set; }
        
        public string? DoctorName { get; set; }
        
        public string? Medication { get; set; }
        
        public string? Dosage { get; set; }
        
        public string? Instructions { get; set; }
        
        public decimal? TotalCost { get; set; }
        
        public bool? IsUrgent { get; set; }
    }

    public class PrescriptionItemRequest
    {
        [Required]
        public int InventoryItemId { get; set; }
        
        [Required]
        public string MedicationName { get; set; } = string.Empty;
        
        [Required]
        public string Dosage { get; set; } = string.Empty;
        
        [Required]
        public string Frequency { get; set; } = string.Empty;
        
        [Required]
        public int Duration { get; set; }
        
        public string DurationUnit { get; set; } = string.Empty;
        
        public string Instructions { get; set; } = string.Empty;
        
        public int Quantity { get; set; }
        
        public bool IsSubstitutable { get; set; }
        
        // Legacy properties for backward compatibility
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class CreatePrescriptionItemRequest
    {
        public int? ProductId { get; set; }
        public int? InventoryItemId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(200)]
        public string? MedicationName { get; set; }

        [StringLength(100)]
        public string? Dosage { get; set; }

        [StringLength(200)]
        public string? Instructions { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public int Duration { get; set; }
    }

    // Patient Request DTOs
    public class CreatePatientRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        public DateTime? DateOfBirth { get; set; }
        
        public string Gender { get; set; } = string.Empty;
        
        public string PhoneNumber { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        public string City { get; set; } = string.Empty;
        
        public string Province { get; set; } = string.Empty;
        
        public string PostalCode { get; set; } = string.Empty;
        
        public string NationalId { get; set; } = string.Empty;
        
        public string IdNumber { get; set; } = string.Empty; // Legacy property
        
        public string InsuranceNumber { get; set; } = string.Empty;
        
        public string EmergencyContactName { get; set; } = string.Empty;
        
        public string EmergencyContactPhone { get; set; } = string.Empty;
        
        public List<string> Allergies { get; set; } = new();
        
        public List<string> MedicalConditions { get; set; } = new();
        
        public string MedicalHistory { get; set; } = string.Empty; // Legacy property
        
        public List<string> CurrentMedications { get; set; } = new();
        
        public string BloodType { get; set; } = string.Empty;
        
        public string MaritalStatus { get; set; } = string.Empty;
        
        public string Occupation { get; set; } = string.Empty;
        
        public int? BranchId { get; set; }
        
        // Computed properties for backward compatibility
        public string Name => $"{FirstName} {LastName}";
        
        public string Phone => PhoneNumber;
    }

    // Sale Request DTOs
    public class SaleRequest
    {
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        public List<SaleItemRequest> Items { get; set; } = new();
        
        public decimal DiscountAmount { get; set; }
        
        public string DiscountReason { get; set; } = string.Empty;
        
        public string PaymentMethod { get; set; } = string.Empty;
        
        public string PaymentReference { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        
        public int? CashierId { get; set; }
        
        public int? BranchId { get; set; }
    }

    public class SaleItemRequest
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public decimal DiscountAmount { get; set; }
        
        public string Notes { get; set; } = string.Empty;
    }

    // Common Result DTOs
    public class CsvImportResult
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<ImportError> DetailedErrors { get; set; } = new();
        public bool IsSuccess => FailedCount == 0;
        
        // Legacy property for backward compatibility
        public int ImportedCount => SuccessCount;
    }

    public class ImportError
    {
        public int RowNumber { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string RowData { get; set; } = string.Empty;
    }

    public class SaleResult
    {
        public bool Success { get; set; }
        public int SaleId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public DateTime SaleDate { get; set; }
        
        // Legacy properties for backward compatibility
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RejectPrescriptionRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        
        public bool NotifyDoctor { get; set; } = true;
        
        public bool NotifyPatient { get; set; } = false;
    }

    // Usage Tracking DTOs
    public class UsageAlert
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // warning, critical
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Properties for notification service compatibility
        public double Current { get; set; }
        public double Limit { get; set; }
        public double Percentage { get; set; }
    }
}
