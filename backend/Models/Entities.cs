using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models
{
    // TimeOnly value object for time-only properties
    public class TimeOnly
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }

        public TimeOnly(int hours, int minutes)
        {
            Hours = hours;
            Minutes = minutes;
        }

        public override string ToString()
        {
            return $"{Hours:D2}:{Minutes:D2}";
        }
    }

    // Core Entities
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(200)]
        public string? PharmacyName { get; set; }

        [StringLength(100)]
        public string? AdminName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        [StringLength(100)]
        public string? ZambiaRegNumber { get; set; }

        public int? SubscriptionPlan { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    }

    public class Branch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Region { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? ManagerName { get; set; }

        [StringLength(20)]
        public string? ManagerPhone { get; set; }

        [StringLength(200)]
        public string? OperatingHours { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyRevenue { get; set; }

        public int StaffCount { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    }

    public class UserAccount
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;

        public DateTime? LastLogin { get; set; }

        [StringLength(100)]
        public string? NormalizedEmail { get; set; }

        [StringLength(256)]
        public string? PasswordHash { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(20)]
        public string Role { get; set; } = "Cashier";

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int? SessionTimeoutMinutes { get; set; } = 30;

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    }

    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string ParentCategory { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? GenericName { get; set; }

        [StringLength(100)]
        public string? BrandName { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

        public int ReorderLevel { get; set; } = 10;

        public int Stock { get; set; } = 0;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }

    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<Sale> Sales { get; set; }
    }

    public class Sale
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        public int? BranchId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; }

        [StringLength(100)]
        public string? PaymentDetails { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal CashReceived { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Change { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ChangeGiven { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "completed";

        [StringLength(500)]
        public string? RefundReason { get; set; }

        public DateTime? RefundedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public virtual Sale Sale { get; set; } = null!;
        public virtual Product Product { get; set; }
    }

    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? InventoryItemName { get; set; }

        [StringLength(100)]
        public string? GenericName { get; set; }

        [StringLength(100)]
        public string? BrandName { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

        public int ReorderLevel { get; set; } = 10;

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? BatchNumber { get; set; }

        public DateTime? ManufactureDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        [StringLength(100)]
        public string? ZambiaRegNumber { get; set; }

        [StringLength(50)]
        public string? PackingType { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
    }

    // Daybook Entities
    public class DaybookTransaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<DaybookTransactionItem> Items { get; set; }
    }

    public class DaybookTransactionItem
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public int? ProductId { get; set; }

        [StringLength(200)]
        public string? ProductName { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Total { get; set; }

        // Navigation properties
        public virtual DaybookTransaction Transaction { get; set; }
        public virtual Product? Product { get; set; }
    }

    // Additional entities needed for compilation
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(50)]
        public string? IdNumber { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? MedicalHistory { get; set; }

        [StringLength(100)]
        public string? Allergies { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        public DateTime? DateOfBirth { get; set; }
        
        // Additional properties for controller compatibility
        [StringLength(50)]
        public string? PatientId { get; set; }
        
        // Property for ApplicationDbContext compatibility
        public virtual ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; }
    }

    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }

    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PrescriptionNumber { get; set; }

        public int PatientId { get; set; }

        public int? DoctorId { get; set; }

        [StringLength(200)]
        public string? DoctorName { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string? DoctorRegistrationNumber { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        public string? Medication { get; set; }

        [StringLength(100)]
        public string? Dosage { get; set; }

        public DateTime? PrescriptionDate { get; set; }
        public DateTime? FilledDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalCost { get; set; }

        public bool IsUrgent { get; set; } = false;

        [StringLength(50)]
        public string? RxNumber { get; set; }

        [StringLength(200)]
        public string? PatientName { get; set; }

        [StringLength(50)]
        public string? PatientIdNumber { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Patient Patient { get; set; }
        public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }

    public class PrescriptionItem
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public int ProductId { get; set; }

        public int? InventoryItemId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(200)]
        public string? MedicationName { get; set; }

        [StringLength(100)]
        public string Dosage { get; set; }

        [StringLength(200)]
        public string Instructions { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int Duration { get; set; }

        // Navigation properties
        public virtual Prescription Prescription { get; set; }
        public virtual Product Product { get; set; }
        public virtual InventoryItem? InventoryItem { get; set; }
    }

    public class Pharmacy
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal YearlyPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int MaxUsers { get; set; } = 5;
        public int MaxBranches { get; set; } = 3;
        public int MaxStorageGB { get; set; } = 10;
        public int MaxProducts { get; set; } = 1000;

        [StringLength(1000)]
        public string? Features { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public bool IncludesSupport { get; set; } = false;
        public bool IncludesAdvancedReporting { get; set; } = false;
        public bool IncludesAPIAccess { get; set; } = false;
        public int MaxTransactions { get; set; } = 1000;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Subscription> Subscriptions { get; set; }
    }

    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int SubscriptionPlanId { get; set; }

        public int? PharmacyId { get; set; }

        // Alias for compatibility
        public int PlanId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
        public virtual SubscriptionPlan? Plan { get; set; }
        public virtual Pharmacy? Pharmacy { get; set; }
    }

    public class ActivityLog
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(100)]
        public string Action { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(450)]
        public string? User { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class UserSession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(200)]
        public string SessionToken { get; set; }

        public DateTime ExpiresAt { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(200)]
        public string? DeviceInfo { get; set; }

        [StringLength(100)]
        public string? Browser { get; set; }

        [StringLength(450)]
        public string? User { get; set; }

        [StringLength(500)]
        public string? Token { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Shift Management Entities
    public class Shift
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        // Additional properties for controller compatibility
        [StringLength(100)]
        public string ShiftName { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public DateTime? ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }

        public TimeSpan? ScheduledDuration { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<ShiftAssignment> Assignments { get; set; }
    }

    public class ShiftAssignment
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int EmployeeId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }
        
        // Additional properties for controller compatibility
        [StringLength(100)]
        public string? Position { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public DateTime AssignmentDate { get; set; }

        // Navigation properties
        public virtual Shift Shift { get; set; } = null!;

        public DateTime? ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }

        [StringLength(20)]
        public string AssignmentStatus { get; set; } = "Assigned";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }

        public long? TotalWorkedMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual UserAccount Employee { get; set; }
    }

    // Billing Entities
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";

        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }

    public class CreditNote
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CreditNoteNumber { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? InvoiceId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Invoice? Invoice { get; set; }
    }

    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentNumber { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? InvoiceId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; }

        [StringLength(500)]
        public string? PaymentDetails { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Completed";

        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Invoice? Invoice { get; set; }
    }

    // Stock Management
    public class StockTransaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionNumber { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        [StringLength(20)]
        public string TransactionType { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalCost { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Product Product { get; set; }
    }

    // Pharmacy Compliance
    public class ControlledSubstance
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string SubstanceName { get; set; }

        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? GenericName { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; }

        [StringLength(100)]
        public string? Schedule { get; set; }

        [StringLength(100)]
        public string? RegistrationNumber { get; set; }

        [StringLength(100)]
        public string? Classification { get; set; }

        public int CurrentStock { get; set; }

        public int ReorderLevel { get; set; } = 5;

        public DateTime? LastDispensed { get; set; }

        public decimal ComplianceScore { get; set; } = 100.0m;

        [StringLength(1000)]
        public string? AuditHistory { get; set; }

        public DateTime? LastAudit { get; set; }
        public DateTime? NextAuditDue { get; set; }
        public int MonthlyDispensed { get; set; } = 0;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    // Employee Entity
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(50)]
        public string? EmployeeNumber { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Position { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(256)]
        public string? PasswordHash { get; set; }
        [StringLength(20)]
        public int? SessionTimeoutMinutes { get; set; }
        [StringLength(100)]
        public string Role { get; set; } = "Cashier";

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; }
    }

    // Additional Entities for System
    public class ReportSchedule
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ReportName { get; set; }

        [StringLength(20)]
        public string ReportType { get; set; }

        [StringLength(20)]
        public string ScheduleType { get; set; }

        [StringLength(20)]
        public string Frequency { get; set; } = string.Empty;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(200)]
        public string? RecipientEmail { get; set; }

        public DateTime? NextRunDate { get; set; }

        public string? Parameters { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
    }

    public class UserBranch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch Branch { get; set; }
        public virtual UserAccount? User { get; set; }
        public virtual Role? UserRole { get; set; }
        public virtual Permission? Permission { get; set; }
    }

    // RBAC Entities
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string Level { get; set; } = "Medium";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsGlobal { get; set; } = false;
        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }
        public virtual ICollection<TenantRole> TenantRoles { get; set; }
    }

    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string RiskLevel { get; set; } = "Medium";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }

    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        public virtual Role Role { get; set; }
        public virtual Permission Permission { get; set; }
    }

    public class UserRole
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public int RoleId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        public virtual UserAccount User { get; set; }
        public virtual Role Role { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    public class TenantRole
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; }

        public int RoleId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastUsedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; }
        public virtual Role Role { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(100)]
        public string? ActionUrl { get; set; }

        [StringLength(50)]
        public string? ActionText { get; set; }

        [StringLength(1000)]
        public string? Metadata { get; set; }

        public bool IsRead { get; set; } = false;
        public bool IsGlobal { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public virtual UserAccount User { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
    }

    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; }

        [Required]
        [StringLength(1000)]
        public string Value { get; set; }

        [Required]
        [StringLength(20)]
        public string DataType { get; set; }

        [Required]
        [StringLength(20)]
        public string Environment { get; set; }

        [StringLength(450)]
        public string? UpdatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SettingsAuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        [Required]
        [StringLength(2000)]
        public string OldValue { get; set; }

        [Required]
        [StringLength(2000)]
        public string NewValue { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Application Feature Management
    public class ApplicationFeature
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(50)]
        public string? Module { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsSystem { get; set; } = false;

        public bool BasicPlan { get; set; } = false;
        public bool ProfessionalPlan { get; set; } = false;
        public bool EnterprisePlan { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; }
    }

    public class SubscriptionPlanFeature
    {
        public int Id { get; set; }
        public int SubscriptionPlanId { get; set; }
        public int ApplicationFeatureId { get; set; }

        public bool IsEnabled { get; set; } = true;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(450)]
        public string? User { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SubscriptionPlan SubscriptionPlan { get; set; }
        public virtual ApplicationFeature ApplicationFeature { get; set; }
    }

    public class ControlledSubstanceAudit
    {
        public int Id { get; set; }

        public int ControlledSubstanceId { get; set; }

        public int SubstanceId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(100)]
        public string Action { get; set; }

        [StringLength(100)]
        public string? RequiredAction { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        [StringLength(1000)]
        public string? DiscrepancyDetails { get; set; }

        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }

        public int OldStock { get; set; }
        public int NewStock { get; set; }

        public int PreviousStock { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? Finding { get; set; }

        public DateTime? AuditDate { get; set; }

        // Navigation properties
        public virtual ControlledSubstance? ControlledSubstance { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    // Application Settings
    public class AppSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; }

        [StringLength(1000)]
        public string? Value { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsEncrypted { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;

        [StringLength(450)]
        public string? UpdatedBy { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(20)]
        public string DataType { get; set; }

        [StringLength(20)]
        public string Environment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? BusinessName { get; set; }

        [StringLength(100)]
        public string? TradeName { get; set; }

        [StringLength(50)]
        public string? SupplierCode { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(50)]
        public string? PrimaryPhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public int NumberOfEmployees { get; set; } = 0;

        public bool IsPreferredSupplier { get; set; } = false;
        
        // Property for ApplicationDbContext compatibility
        public bool IsPreferred { get; set; } = false;

        public bool IsZambianRegistered { get; set; } = false;

        public bool HasGMPCertificate { get; set; } = false;
        
        // Additional properties for controller compatibility
        public bool GmpCertified { get; set; } = false;
        public bool IsoCertified { get; set; } = false;
        public DateTime? CertificationExpiryDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Additional properties for ApplicationDbContext configuration
        [StringLength(100)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? TaxIdentificationNumber { get; set; }

        [StringLength(100)]
        public string? PharmacyLicenseNumber { get; set; }

        [StringLength(100)]
        public string? DrugSupplierLicense { get; set; }

        [StringLength(50)]
        public string? ContactPersonTitle { get; set; }

        [StringLength(20)]
        public string? SecondaryPhoneNumber { get; set; }

        [StringLength(100)]
        public string? AlternativeEmail { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(500)]
        public string? PhysicalAddress { get; set; }

        [StringLength(500)]
        public string? PostalAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? BusinessType { get; set; }

        [StringLength(100)]
        public string? Industry { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? AnnualRevenue { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? BankAccountNumber { get; set; }

        [StringLength(100)]
        public string? BankAccountName { get; set; }

        [StringLength(100)]
        public string? BankBranch { get; set; }

        [StringLength(20)]
        public string? BankCode { get; set; }

        [StringLength(20)]
        public string? SwiftCode { get; set; }

        [StringLength(50)]
        public string? PaymentTerms { get; set; } = "Net 30";

        [Column(TypeName = "decimal(15,2)")]
        public decimal CreditLimit { get; set; } = 0.00m;

        public int CreditPeriod { get; set; } = 30;

        [StringLength(100)]
        public string? DiscountTerms { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal EarlyPaymentDiscount { get; set; } = 0.00m;

        [StringLength(50)]
        public string? SupplierCategory { get; set; }

        [StringLength(20)]
        public string? SupplierStatus { get; set; } = "Active";

        // Missing properties for controller compatibility
        public int? YearsInOperation { get; set; }
        public bool IsBlacklisted { get; set; } = false;
        public bool ZambianRegistered { get; set; } = false;
        public bool IsBillingContact { get; set; } = false;
        public bool IsTechnicalContact { get; set; } = false;

        [StringLength(20)]
        public string? PriorityLevel { get; set; } = "Medium";

        [StringLength(500)]
        public string? BlacklistReason { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal OnTimeDeliveryRate { get; set; } = 0.00m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal QualityRating { get; set; } = 0.00m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal PriceCompetitiveness { get; set; } = 0.00m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal OverallRating { get; set; } = 0.00m;

        [StringLength(20)]
        public string? RegulatoryComplianceStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SupplierProduct> Products { get; set; } = new List<SupplierProduct>();
        public virtual ICollection<SupplierContact> Contacts { get; set; } = new List<SupplierContact>();
    }

    public class SupplierProduct
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }

        public int ProductId { get; set; }

        [StringLength(100)]
        public string? SupplierProductCode { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal SupplierPrice { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal MinimumOrderQuantity { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal MaximumOrderQuantity { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal OrderMultiples { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal MinimumOrderValue { get; set; }

        public bool IsAvailable { get; set; } = true;

        [StringLength(50)]
        public string? Currency { get; set; } = "ZMW";

        [StringLength(20)]
        public string? QualityGrade { get; set; }

        [StringLength(100)]
        public string? BatchNumber { get; set; }

        public DateTime? ManufactureDate { get; set; }

        public int? LeadTimeDays { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime? ExpiryDate { get; set; }

        [StringLength(500)]
        public string? StorageRequirements { get; set; }

        [StringLength(100)]
        public string? SupplierCatalogNumber { get; set; }

        [StringLength(100)]
        public string? SupplierBarcode { get; set; }

        [StringLength(500)]
        public string? PackagingInformation { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? WeightPerUnit { get; set; }

        [StringLength(200)]
        public string? Dimensions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Additional properties for controller compatibility
        public int? InventoryItemId { get; set; }
        
        [StringLength(200)]
        public string? SupplierProductName { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal UnitCost { get; set; }

        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual InventoryItem? InventoryItem { get; set; }
    }

    public class SupplierContact
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }

        [Required]
        [StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Position { get; set; } = string.Empty;
        
        // Property for ApplicationDbContext compatibility
        [StringLength(50)]
        public string? ContactTitle { get; set; }
        
        [StringLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string MobileNumber { get; set; } = string.Empty;

        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string ContactType { get; set; } = "General"; // "Sales", "Technical", "General", "Accounts"
        
        // Property for ApplicationDbContext compatibility
        [StringLength(50)]
        public string? Department { get; set; }

        public bool IsPrimaryContact { get; set; } = false;
        
        // Property for ApplicationDbContext compatibility
        public bool IsPrimary { get; set; } = false;
        
        // Property for ApplicationDbContext compatibility
        public bool IsOrderContact { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        
        // Property for ApplicationDbContext compatibility
        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsBillingContact { get; set; } = false;
        public bool IsTechnicalContact { get; set; } = false;

        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
    }

    public class ShiftSwapRequest
    {
        public int Id { get; set; }

        [Required]
        public int OriginalShiftId { get; set; }

        [Required]
        [StringLength(450)]
        public string OriginalUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string TargetUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed

        [StringLength(450)]
        public string? ApprovedBy { get; set; }

        [StringLength(1000)]
        public string? ApprovalNotes { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ShiftAssignment OriginalShift { get; set; } = null!;
        public virtual ShiftAssignment TargetShift { get; set; } = null!;
        public virtual UserAccount OriginalUser { get; set; } = null!;
        public virtual UserAccount TargetUser { get; set; } = null!;
    }

    // Missing entities for controller compatibility
    public class SearchHistory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [Required]
        [StringLength(500)]
        public string SearchTerm { get; set; } = string.Empty;

        [StringLength(50)]
        public string SearchType { get; set; } = "General"; // General, Medical, Product, Patient

        [StringLength(1000)]
        public string? SearchResults { get; set; }

        public int ResultCount { get; set; } = 0;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
        
        // Properties for ApplicationDbContext compatibility
        public bool IsActive { get; set; } = true;
        
        [StringLength(100)]
        public string? Query { get; set; }
        
        [StringLength(50)]
        public string? Source { get; set; }
        
        [StringLength(1000)]
        public string? Details { get; set; }

        // Navigation properties
        public virtual UserAccount? User { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    public class TimeOffRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(6)]
        public string? TenantId { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string? StartTime { get; set; }
        public string? EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(50)]
        public string TimeOffType { get; set; } = "Annual Leave"; // Annual Leave, Sick Leave, Personal, Maternity, Paternity

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        [StringLength(20)]
        public string RequestType { get; set; } = "Leave"; // Leave, Sick, Personal

        [StringLength(450)]
        public string? ApprovedBy { get; set; }

        [StringLength(1000)]
        public string? ApprovalNotes { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        public int? ApprovedDays { get; set; }
        public bool IsPaid { get; set; } = true;

        [StringLength(500)]
        public string? Attachments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual UserAccount User { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
    }

    public class BackupLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BackupId { get; set; } = string.Empty;

        [StringLength(6)]
        public string? TenantId { get; set; }

        [Required]
        [StringLength(20)]
        public string BackupType { get; set; } = "Full"; // Full, Incremental, Differential

        [Required]
        public long BackupSize { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Completed"; // InProgress, Completed, Failed

        [StringLength(1000)]
        public string? FilePath { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    // Enums for Role-Based Access Control

    public class UsageRecord
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [StringLength(6)]
        public string? TenantId { get; set; }
        [StringLength(100)]
        public string Feature { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        
        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(1000)]
        public string? MetadataJson { get; set; }
    }

    public class AccessLog
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        public string Role { get; set; } = string.Empty;
        
        [StringLength(6)]
        public string? TenantId { get; set; }
        
        public int? BranchId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;
        
        public string Resource { get; set; } = string.Empty;
        
        public string ResourceId { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Controller { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string HttpMethod { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Status { get; set; } = "Success";
        
        [StringLength(1000)]
        public string? UserAgent { get; set; }
        
        [StringLength(45)]
        public string? IpAddress { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string Details { get; set; } = string.Empty;
        
        public bool IsImpersonated { get; set; }
        
        public string ImpersonatedByUserId { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? MetadataJson { get; set; }
    }

    public class AdditionalUserPurchase
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [StringLength(6)]
        public string? TenantId { get; set; }
        [StringLength(100)]
        public string PurchaseType { get; set; } = string.Empty;
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        
        // Additional properties for ApplicationDbContext compatibility
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerUser { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }
        
        public int NumberOfUsers { get; set; } = 1;
        
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(20)]
        public string Status { get; set; } = "active";
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    public class CategorySync
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string SourceCategory { get; set; } = string.Empty;
        [StringLength(100)]
        public string TargetCategory { get; set; } = string.Empty;
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        public DateTime SyncDate { get; set; } = DateTime.UtcNow;
    }

    public class TenantCategory
    {
        public int Id { get; set; }
        [Required]
        public int TenantId { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
    }

    public class PharmacistProfile
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [StringLength(6)]
        public string? TenantId { get; set; }
        
        // Profile information
        [StringLength(100)]
        public string? FirstName { get; set; }
        [StringLength(100)]
        public string? LastName { get; set; }
        [StringLength(255)]
        public string? Email { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        [StringLength(100)]
        public string LicenseNumber { get; set; } = string.Empty;
        
        // Settings
        public bool EmailNotifications { get; set; } = false;
        public bool ClinicalAlerts { get; set; } = false;
        public int SessionTimeout { get; set; } = 30;
        public string Language { get; set; } = "en";
        public bool TwoFactorEnabled { get; set; } = false;
        
        // Property for ApplicationDbContext compatibility
        public string? TwoFactorSecret { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Additional fields
        [StringLength(20)]
        public string Specialization { get; set; } = string.Empty;
        public int YearsExperience { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Signature { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PasswordChangedAt { get; set; }
        public bool ForcePasswordChange { get; set; } = false;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        
        // Navigation properties
        public virtual UserAccount? User { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    public class EnhancedUserSession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? ImpersonatedByUserId { get; set; }

        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        public bool IsImpersonated { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime ExpiresAt { get; set; }

        public DateTime ImpersonatedAt { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // Additional properties for impersonation
        public string? Role { get; set; }
        public string? TenantId { get; set; }
        public string? BranchId { get; set; }
        public string? DeviceInfo { get; set; }
        public string? Browser { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OriginalRole { get; set; }
        public string? OriginalTenantId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual UserAccount User { get; set; } = null!;

        [ForeignKey("ImpersonatedByUserId")]
        public virtual UserAccount? ImpersonatedByUser { get; set; }
    }

    public class DrugInteraction
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Drug1 { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string Drug2 { get; set; } = string.Empty;
        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = string.Empty; // Major, Moderate, Minor
        [StringLength(1000)]
        public string? Description { get; set; }
        [StringLength(500)]
        public string? Recommendation { get; set; }
        
        // Properties for ApplicationDbContext compatibility
        [StringLength(100)]
        public string? Medication1 { get; set; }
        
        [StringLength(100)]
        public string? Medication2 { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MedicationAllergy
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Medication { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string Allergen { get; set; } = string.Empty;
        [Required]
        [StringLength(20)]
        public string ReactionType { get; set; } = string.Empty; // Mild, Moderate, Severe
        [StringLength(1000)]
        public string? Symptoms { get; set; }
        [StringLength(500)]
        public string? CrossReactivity { get; set; }
        
        // Properties for ApplicationDbContext compatibility
        [StringLength(20)]
        public string Severity { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Reaction { get; set; }
        
        [StringLength(1000)]
        public string? Recommendation { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ClinicalGuideline
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        [StringLength(1000)]
        public string? Description { get; set; }
        [StringLength(1000)]
        public string? Recommendations { get; set; }
        
        // Properties for ApplicationDbContext compatibility
        [StringLength(500)]
        public string? Condition { get; set; }
        
        [StringLength(500)]
        public string? Medication { get; set; }
        
        [StringLength(1000)]
        public string? Recommendation { get; set; }
        
        [StringLength(100)]
        public string? Source { get; set; }
        [StringLength(20)]
        public string? EvidenceLevel { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ClinicalNote
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string PatientId { get; set; } = string.Empty;
        [Required]
        [StringLength(450)]
        public string DoctorId { get; set; } = string.Empty;
        [StringLength(6)]
        public string? TenantId { get; set; }
        [Required]
        [StringLength(1000)]
        public string Note { get; set; } = string.Empty;
        
        // Property for ApplicationDbContext compatibility
        [StringLength(50)]
        public string? NoteType { get; set; }
        
        // Property for ApplicationDbContext compatibility
        [StringLength(1000)]
        public string? Content { get; set; }
        
        // Property for ApplicationDbContext compatibility
        [StringLength(500)]
        public string? Diagnosis { get; set; }
        
        // Property for ApplicationDbContext compatibility
        [StringLength(500)]
        public string? Symptoms { get; set; }
        
        // Property for ApplicationDbContext compatibility
        [StringLength(500)]
        public string? Treatment { get; set; }
        
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Diagnosis, Treatment, Follow-up
        public DateTime NoteDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        
        // Property for ApplicationDbContext compatibility
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;
        
        [ForeignKey("DoctorId")]
        public virtual UserAccount Doctor { get; set; } = null!;
        
        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }
    }

    public static class SystemPermissions
    {
        public const string USER_READ = "USER_READ";
        public const string USER_CREATE = "USER_CREATE";
        public const string USER_UPDATE = "USER_UPDATE";
        public const string USER_DELETE = "USER_DELETE";
        public const string USER_RESET_PASSWORD = "USER_RESET_PASSWORD";
        public const string IMPERSONATE_USER = "IMPERSONATE_USER";
        public const string VIEW_IMPERSONATION_LOGS = "VIEW_IMPERSONATION_LOGS";
        public const string SYSTEM_SETTINGS = "SYSTEM_SETTINGS";
        public const string VIEW_SECURITY_ALERTS = "VIEW_SECURITY_ALERTS";
        public const string VIEW_COMPLIANCE_REPORTS = "VIEW_COMPLIANCE_REPORTS";
    }

    public class HelpCategory
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [StringLength(500)]
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        
        // Properties for ApplicationDbContext compatibility
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive
        
        [StringLength(50)]
        public string? Order { get; set; }
        
        [StringLength(50)]
        public string? CategoryId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Icon { get; set; } = "default-icon";
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<HelpArticle> Articles { get; set; } = new List<HelpArticle>();
    }

    public class HelpArticle
    {
        public int Id { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Published, Archived
        public int ViewCount { get; set; }
        public int? Order { get; set; }
        
        // Properties for ApplicationDbContext compatibility
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(450)]
        public string? CreatedBy { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ArticleId { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string ReadingTime { get; set; } = "5 min read";
        
        // Properties for ApplicationDbContext compatibility
        [StringLength(50)]
        public string? CategoryIdentifier { get; set; }
        
        // Navigation properties
        public virtual HelpCategory Category { get; set; } = null!;
        public virtual ICollection<HelpFeedback> Feedback { get; set; } = new List<HelpFeedback>();
    }

    public class HelpFeedback
    {
        public int Id { get; set; }
        [Required]
        public int ArticleId { get; set; }
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        [StringLength(1000)]
        public string? Feedback { get; set; }
        [StringLength(10)]
        public string Rating { get; set; } = string.Empty;
        
        // Properties for ApplicationDbContext compatibility
        [StringLength(6)]
        public string? TenantId { get; set; }
        
        [StringLength(500)]
        public string? Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual HelpArticle Article { get; set; } = null!;
        public virtual UserAccount User { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
    }

    // Enums for Role-Based Access Control
    public static class RoleHierarchy
    {
        public static int GetHierarchyLevel(this UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => 1,
                UserRoleEnum.TenantAdmin => 2,
                UserRoleEnum.Pharmacist => 3,
                UserRoleEnum.Cashier => 4,
                UserRoleEnum.Sales => 5,
                _ => 99
            };
        }
    }

    public enum UserRoleEnum
    {
        SuperAdmin = 1,
        TenantAdmin = 2,
        Pharmacist = 3,
        Cashier = 4,
        Sales = 5,
        Operations = 6
    }

    public class ImpersonationLog
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string SuperAdminUserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(450)]
        public string ImpersonatedUserId { get; set; } = string.Empty;
        
        [StringLength(6)]
        public string? TenantId { get; set; }
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Ended, Expired
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(1000)]
        public string? Reason { get; set; }
        
        [StringLength(45)]
        public string? IpAddress { get; set; }
        
        [StringLength(100)]
        public string? TargetIpAddress { get; set; }
        
        [StringLength(1000)]
        public string? UserAgent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(450)]
        public string? AdminUserId { get; set; }
        
        [StringLength(450)]
        public string? TargetUserId { get; set; }
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? ImpersonatedRole { get; set; }
        
        // Navigation properties
        public virtual UserAccount? AdminUser { get; set; }
        public virtual UserAccount? TargetUser { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }
}
