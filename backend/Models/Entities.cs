using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static UmiHealthPOS.Models.UserRole;
using System.Linq;
using System.Threading.Tasks;

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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (temporarily commented for migration)
        // public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
        // public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        // public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        // public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        // public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        // public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        // public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        // public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        // public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        // public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        // public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        // public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        // public virtual ICollection<DaybookTransaction> DaybookTransactions { get; set; } = new List<DaybookTransaction>();
        // public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        // public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
        // public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        // public virtual ICollection<ControlledSubstance> ControlledSubstances { get; set; } = new List<ControlledSubstance>();
        // public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
        // public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        // public virtual ICollection<ControlledSubstanceAudit> ControlledSubstanceAudits { get; set; } = new List<ControlledSubstanceAudit>();
        // public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        // public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
        // public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        // public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        // public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        // public virtual ICollection<TenantRole> TenantRoles { get; set; } = new List<TenantRole>();
        // public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        // public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
        // public virtual ICollection<SettingsAuditLog> SettingsAuditLogs { get; set; } = new List<SettingsAuditLog>();
        // public virtual ICollection<AppSetting> AppSettings { get; set; } = new List<AppSetting>();
        // public virtual ICollection<ApplicationFeature> ApplicationFeatures { get; set; } = new List<ApplicationFeature>();
        // public virtual ICollection<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; } = new List<SubscriptionPlanFeature>();
        // public virtual ICollection<CategorySync> CategorySyncs { get; set; } = new List<CategorySync>();
        // public virtual ICollection<TenantCategory> TenantCategories { get; set; } = new List<TenantCategory>();
        // public virtual ICollection<SupplierContact> SupplierContacts { get; set; } = new List<SupplierContact>();
        // public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
        // public virtual ICollection<PharmacistProfile> PharmacistProfiles { get; set; } = new List<PharmacistProfile>();
        // public virtual ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
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
        public required string Email { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = UserRoleEnum.Cashier.ToString();

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        [StringLength(256)]
        public string? PasswordHash { get; set; }

        [StringLength(256)]
        public string? NormalizedEmail { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;

        public DateTime? LastLogin { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(512)]
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    }

    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

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

        [StringLength(100)]
        public string? Barcode { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; } = "Each";

        public bool RequiresPrescription { get; set; } = false;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
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
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string? PaymentDetails { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal CashReceived { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Change { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ChangeGiven { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "completed";

        [StringLength(500)]
        public string? RefundReason { get; set; }

        public DateTime? RefundedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
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
        public virtual Product? Product { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? IdNumber { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(10,2)")]
        public decimal CreditLimit { get; set; } = 0.00m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal OutstandingBalance { get; set; } = 0.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }

    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

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
        public required string TransactionNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public required string PaymentMethod { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<DaybookTransactionItem> Items { get; set; } = new List<DaybookTransactionItem>();
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
        public virtual DaybookTransaction Transaction { get; set; } = null!;
        public virtual Product? Product { get; set; }
    }

    // Additional entities needed for compilation
    public class Patient
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }

    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PrescriptionNumber { get; set; } = string.Empty;

        public int PatientId { get; set; }

        [StringLength(200)]
        public string DoctorName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DoctorRegistrationNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? Medication { get; set; }

        [StringLength(100)]
        public string? Dosage { get; set; }

        [StringLength(200)]
        public string? Instructions { get; set; }

        public DateTime? PrescriptionDate { get; set; }
        public DateTime? FilledDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
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
        public string Dosage { get; set; } = string.Empty;

        [StringLength(200)]
        public string Instructions { get; set; } = string.Empty;

        public DateTime? ExpiryDate { get; set; }

        public int Duration { get; set; }

        // Navigation properties
        public virtual Prescription Prescription { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual InventoryItem? InventoryItem { get; set; }
    }

    public class Pharmacy
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

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
        public int MaxProducts { get; set; } = 500;
        public int MaxTransactions { get; set; } = 1000;
        public int MaxStorageGB { get; set; } = 10;

        [StringLength(1000)]
        public string? Features { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
        public virtual SubscriptionPlan? Plan { get; set; }
        public virtual Pharmacy? Pharmacy { get; set; }
        public virtual ICollection<SubscriptionHistory> SubscriptionHistory { get; set; } = new List<SubscriptionHistory>();
    }

    public class ActivityLog
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(50)]
        public required string Category { get; set; }

        [StringLength(100)]
        public required string Action { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(450)]
        public string? UserName { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class UserSession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public required string UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(200)]
        public string SessionToken { get; set; } = string.Empty;

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

        // Enhanced device tracking fields
        [StringLength(100)]
        public string? DeviceType { get; set; } // Mobile, Desktop, Tablet

        [StringLength(200)]
        public string? DeviceId { get; set; } // Unique device identifier

        [StringLength(100)]
        public string? Platform { get; set; } // Windows, iOS, Android, etc.

        [StringLength(100)]
        public string? Location { get; set; } // Geographic location (optional)

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActivityAt { get; set; } = DateTime.UtcNow;
    }

    // Shift Management Entities
    public class Shift
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ShiftName { get; set; }

        public TimeOnly StartTime { get; set; } = new TimeOnly(0, 0);
        public TimeOnly EndTime { get; set; } = new TimeOnly(0, 0);

        public DateTime? ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }

        public TimeSpan? ScheduledDuration { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
    }


    // Billing Entities
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

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

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class CreditNote
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CreditNoteNumber { get; set; } = string.Empty;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? InvoiceId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
        public string PaymentNumber { get; set; } = string.Empty;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? InvoiceId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public required string PaymentMethod { get; set; }

        [StringLength(500)]
        public string? PaymentDetails { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Completed";

        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
        public required string TransactionNumber { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        [StringLength(20)]
        public required string TransactionType { get; set; }

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
        public virtual Product Product { get; set; } = null!;
    }

    // Pharmacy Compliance
    public class ControlledSubstance
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string SubstanceName { get; set; } = string.Empty;

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
        public required string FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [Required]
        [StringLength(100)]
        public required string Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(50)]
        public string? EmployeeNumber { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? Position { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "Employee";

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(255)]
        public string? PasswordHash { get; set; }

        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
    }

    public class ShiftAssignment
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int EmployeeId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [StringLength(100)]
        public string? Position { get; set; }

        public DateTime AssignmentDate { get; set; }

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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual required Shift Shift { get; set; }
        public virtual required UserAccount Employee { get; set; }
    }

    public class UserBranch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public required string UserId { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int BranchId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Permission { get; set; } = "read";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual required UserAccount? User { get; set; }
        public virtual required Branch? Branch { get; set; }
    }

    // RBAC Entities
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string Level { get; set; } = "Medium";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsGlobal { get; set; } = false;
        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<TenantRole> TenantRoles { get; set; } = new List<TenantRole>();
    }

    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(100)]
        public required string DisplayName { get; set; }

        [Required]
        [StringLength(50)]
        public required string Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string RiskLevel { get; set; } = "Medium";

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual required Role Role { get; set; }
        public virtual required Permission Permission { get; set; }
    }

    public class UserRole
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AssignedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastUsedAt { get; set; }

        // Navigation properties
        public virtual required UserAccount User { get; set; }
        public virtual required Role Role { get; set; }
    }

    public class TenantRole
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public required string TenantId { get; set; }

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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public virtual UserAccount? User { get; set; }
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
        public string Name { get; set; }

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
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // "Active", "Pending", "Inactive"

        [StringLength(100)]
        public string ColorClass { get; set; } = "bg-blue-100 text-blue-600";

        public int ItemCount { get; set; } = 0;

        public int TenantUsage { get; set; } = 0;

        [StringLength(200)]
        public string? RequestedBy { get; set; } // For pending categories

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedBy { get; set; }

        // Navigation properties
        public virtual ICollection<CategorySync> CategorySyncs { get; set; } = new List<CategorySync>();
        public virtual ICollection<TenantCategory> TenantCategories { get; set; } = new List<TenantCategory>();
    }

    public class CategorySync
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string SyncStatus { get; set; } = "Pending";

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public DateTime? SyncAt { get; set; }

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    public class TenantCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public DateTime? AssignedAt { get; set; }

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    // Supplier Management Entity
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SupplierCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; }

        [StringLength(200)]
        public string? TradeName { get; set; }

        [StringLength(100)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? TaxIdentificationNumber { get; set; }

        [StringLength(100)]
        public string? PharmacyLicenseNumber { get; set; }

        [StringLength(100)]
        public string? DrugSupplierLicense { get; set; }

        // Contact Information
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(50)]
        public string? ContactPersonTitle { get; set; }

        [StringLength(20)]
        public string? PrimaryPhoneNumber { get; set; }

        [StringLength(20)]
        public string? SecondaryPhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? AlternativeEmail { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        // Address Information
        [StringLength(500)]
        public string? PhysicalAddress { get; set; }

        [StringLength(500)]
        public string? PostalAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(100)]
        public string? Country { get; set; } = "Zambia";

        [StringLength(20)]
        public string? PostalCode { get; set; }

        // Business Details
        [StringLength(50)]
        public string? BusinessType { get; set; }

        [StringLength(100)]
        public string? Industry { get; set; }

        public int? YearsInOperation { get; set; }

        public int? NumberOfEmployees { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? AnnualRevenue { get; set; }

        // Banking Information
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

        // Payment Terms
        [StringLength(50)]
        public string? PaymentTerms { get; set; } = "Net 30";

        [Column(TypeName = "decimal(15,2)")]
        public decimal CreditLimit { get; set; } = 0.00m;

        public int CreditPeriod { get; set; } = 30;

        [StringLength(100)]
        public string? DiscountTerms { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal EarlyPaymentDiscount { get; set; } = 0.00m;

        // Supplier Classification
        [StringLength(50)]
        public string? SupplierCategory { get; set; }

        [StringLength(20)]
        public string? SupplierStatus { get; set; } = "Active";

        [StringLength(20)]
        public string? PriorityLevel { get; set; } = "Medium";

        public bool IsPreferred { get; set; } = false;

        public bool IsBlacklisted { get; set; } = false;

        [StringLength(500)]
        public string? BlacklistReason { get; set; }

        // Performance Metrics
        [Column(TypeName = "decimal(5,2)")]
        public decimal OnTimeDeliveryRate { get; set; } = 0.00m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal QualityRating { get; set; } = 0.00m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal PriceCompetitiveness { get; set; } = 0.00m;

        [Column(TypeName = "decimal(3,2)")]
        public decimal OverallRating { get; set; } = 0.00m;

        public DateTime? LastPerformanceReview { get; set; }

        // Compliance and Certifications
        public bool ZambianRegistered { get; set; } = false;

        public bool GmpCertified { get; set; } = false;

        public bool IsoCertified { get; set; } = false;

        public DateTime? CertificationExpiryDate { get; set; }

        [StringLength(20)]
        public string? RegulatoryComplianceStatus { get; set; } = "Pending";

        // System Fields
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<SupplierContact> Contacts { get; set; } = new List<SupplierContact>();
        public virtual ICollection<SupplierProduct> Products { get; set; } = new List<SupplierProduct>();
    }

    // Supplier Contact Entity
    public class SupplierContact
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }

        [Required]
        [StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ContactTitle { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        public string? MobileNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public bool IsPrimary { get; set; } = false;
        public bool IsOrderContact { get; set; } = false;
        public bool IsBillingContact { get; set; } = false;
        public bool IsTechnicalContact { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
    }

    // Supplier Product Entity
    public class SupplierProduct
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public int? ProductId { get; set; }
        public int? InventoryItemId { get; set; }

        [StringLength(100)]
        public string? SupplierProductCode { get; set; }

        [StringLength(200)]
        public string? SupplierProductName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Pricing Information
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitCost { get; set; } = 0.00m;

        [StringLength(10)]
        public string? Currency { get; set; } = "ZMW";

        public int MinimumOrderQuantity { get; set; } = 1;
        public int? MaximumOrderQuantity { get; set; }
        public int OrderMultiples { get; set; } = 1;

        // Availability and Lead Time
        public bool IsAvailable { get; set; } = true;
        public int LeadTimeDays { get; set; } = 7;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MinimumOrderValue { get; set; } = 0.00m;

        // Quality and Compliance
        [StringLength(20)]
        public string? QualityGrade { get; set; }

        [StringLength(100)]
        public string? BatchNumber { get; set; }

        public DateTime? ManufactureDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        public string? StorageRequirements { get; set; }

        // Supplier-Specific Details
        [StringLength(100)]
        public string? SupplierCatalogNumber { get; set; }

        [StringLength(100)]
        public string? SupplierBarcode { get; set; }

        [StringLength(200)]
        public string? PackagingInformation { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? WeightPerUnit { get; set; }

        [StringLength(50)]
        public string? Dimensions { get; set; }

        // System Fields
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual Product? Product { get; set; }
        public virtual InventoryItem? InventoryItem { get; set; }
    }

    // Pharmacist Profile Entity
    public class PharmacistProfile
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(100)]
        public required string FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(100)]
        public required string Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        // Pharmacist-specific settings
        public bool EmailNotifications { get; set; } = false;
        public bool ClinicalAlerts { get; set; } = false;
        public int SessionTimeout { get; set; } = 30; // minutes
        public string Language { get; set; } = "en";

        // Two-factor authentication
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }

        // Password management
        public DateTime? PasswordChangedAt { get; set; }
        public bool ForcePasswordChange { get; set; } = false;

        // Profile settings
        [StringLength(500)]
        public string? ProfilePicture { get; set; }

        [StringLength(500)]
        public string? Signature { get; set; }

        // System fields
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual UserAccount? User { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    // Clinical Note Entity
    public class ClinicalNote
    {
        public int Id { get; set; }
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

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Patient? Patient { get; set; }
    }

    // Usage Record Entity for tracking user activities and system usage
    public class UsageRecord
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string ActivityType { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string? Description { get; set; }

        // JSON metadata for additional information
        public string? MetadataJson { get; set; }

        // System fields
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual UserAccount? User { get; set; }
    }

    // Additional User Purchase Entity
    public class AdditionalUserPurchase
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        public int NumberOfUsers { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerUser { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "active"; // active, expired, cancelled

        [StringLength(500)]
        public string? Notes { get; set; }

        // System fields
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }

    // Report Scheduling Entity
    public class ReportSchedule
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Frequency { get; set; } = string.Empty; // Daily, Weekly, Monthly, etc.

        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; } // User who scheduled the report

        [StringLength(100)]
        public string? RecipientEmail { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? NextRunDate { get; set; }

        public DateTime? LastRunDate { get; set; }

        [StringLength(1000)]
        public string? Parameters { get; set; } // JSON string for report parameters

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual Branch? Branch { get; set; }
    }

    // Search History Entity
    public class SearchHistory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string TenantId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Query { get; set; } = string.Empty;

        [StringLength(50)]
        public string SearchType { get; set; } = "general"; // general, medical, product, patient

        [StringLength(20)]
        public string? Source { get; set; } // web, mobile, api

        public int? ResultCount { get; set; }

        [StringLength(1000)]
        public string? Filters { get; set; } // JSON string for search filters

        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [StringLength(200)]
        public string? UserAgent { get; set; }

        // System fields
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
        public virtual UserAccount? User { get; set; }
    }

    // Help & Training System Entities
    public class HelpCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string Icon { get; set; } = string.Empty;

        public int Order { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<HelpArticle> Articles { get; set; } = new List<HelpArticle>();
    }

    public class HelpArticle
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ArticleId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CategoryId { get; set; } = string.Empty;

        public int Order { get; set; }

        [StringLength(50)]
        public string ReadingTime { get; set; } = "5 min read";

        [StringLength(20)]
        public string Status { get; set; } = "Published";

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual HelpCategory Category { get; set; } = null!;
        public virtual ICollection<HelpFeedback> Feedback { get; set; } = new List<HelpFeedback>();
    }

    public class HelpFeedback
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ArticleId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public bool Helpful { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual HelpArticle Article { get; set; } = null!;
        public virtual UserAccount? User { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }

    // Admin Function Entities
    public class TimeOffRequest
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string RequestType { get; set; } = "Leave"; // Leave, Sick, Personal

        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [StringLength(450)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [StringLength(1000)]
        public string? ApprovalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual UserAccount? User { get; set; }
        public virtual UserAccount? Approver { get; set; }
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

        // Navigation properties
        public virtual Tenant? Tenant { get; set; }
    }
}
