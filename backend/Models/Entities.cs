using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [StringLength(100)]
        public string Category { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        public int Stock { get; set; }
        
        [StringLength(50)]
        public string Barcode { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        public int MinStock { get; set; } = 5;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<SaleItem> SaleItems { get; set; }
        public virtual ICollection<StockTransaction> StockTransactions { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(100)]
        public string Email { get; set; }
        
        [StringLength(20)]
        public string Phone { get; set; }
        
        [StringLength(200)]
        public string Address { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Sale> Sales { get; set; }
    }

    public class Sale
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; }
        
        public int? CustomerId { get; set; }
        
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
        public string PaymentDetails { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal CashReceived { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Change { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "completed"; // "completed", "pending", "refunded", "cancelled"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string RefundReason { get; set; }
        
        public DateTime? RefundedAt { get; set; }
        
        public int? BranchId { get; set; }
        
        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; }
    }

    public class SaleItem
    {
        public int Id { get; set; }
        
        [Required]
        public int SaleId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }
        
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }
        
        // Navigation properties
        public virtual Sale Sale { get; set; }
        public virtual Product Product { get; set; }
    }

    public class StockTransaction
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } // "Sale", "Purchase", "Adjustment", "Return"
        
        public int QuantityChange { get; set; }
        
        public int PreviousStock { get; set; }
        
        public int NewStock { get; set; }
        
        [StringLength(200)]
        public string Reason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Product Product { get; set; }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string InventoryItemName { get; set; }
        
        [Required]
        [StringLength(200)]
        public string GenericName { get; set; }
        
        [Required]
        [StringLength(200)]
        public string BrandName { get; set; }
        
        [Column(TypeName = "date")]
        public DateTime ManufactureDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string BatchNumber { get; set; }
        
        [StringLength(100)]
        public string LicenseNumber { get; set; }
        
        [StringLength(100)]
        public string ZambiaRegNumber { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PackingType { get; set; } // "Box", "Bottle", "Packet", "Blister", etc.
        
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }
        
        public int ReorderLevel { get; set; } = 10;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int? BranchId { get; set; }
        
        // Navigation properties
        public virtual ICollection<StockTransaction> StockTransactions { get; set; }
    }

    public class Prescription
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RxNumber { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string PatientName { get; set; }
        
        [StringLength(20)]
        public string PatientIdNumber { get; set; }
        
        [Required]
        [StringLength(200)]
        public string DoctorName { get; set; }
        
        [StringLength(100)]
        public string DoctorRegistrationNumber { get; set; }
        
        [Required]
        [StringLength(300)]
        public string Medication { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Dosage { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Instructions { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // "pending", "filled", "expired", "cancelled"
        
        [Column(TypeName = "date")]
        public DateTime PrescriptionDate { get; set; } = DateTime.Today;
        
        [Column(TypeName = "date")]
        public DateTime? ExpiryDate { get; set; }
        
        [Column(TypeName = "date")]
        public DateTime? FilledDate { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        public bool IsUrgent { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int? BranchId { get; set; }
        
        // Navigation properties
        public virtual Patient Patient { get; set; }
        public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; }
    }

    public class Patient
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [StringLength(20)]
        public string IdNumber { get; set; }
        
        [StringLength(100)]
        public string PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string Email { get; set; }
        
        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string Gender { get; set; }
        
        [StringLength(200)]
        public string Address { get; set; }
        
        [StringLength(100)]
        public string Allergies { get; set; }
        
        [StringLength(500)]
        public string MedicalHistory { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int? BranchId { get; set; }
        
        // Navigation properties
        public virtual ICollection<Prescription> Prescriptions { get; set; }
    }

    public class PrescriptionItem
    {
        public int Id { get; set; }
        
        [Required]
        public int PrescriptionId { get; set; }
        
        [Required]
        public int InventoryItemId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string MedicationName { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Dosage { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Instructions { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Prescription Prescription { get; set; }
        public virtual InventoryItem InventoryItem { get; set; }
    }
}
