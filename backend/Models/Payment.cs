using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealthPOS.Models
{
    public class Quotation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string QuotationNumber { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string? PatientName { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Expired

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime ValidUntil { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
    }

    public class QuotationItem
    {
        public int Id { get; set; }

        public int QuotationId { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public virtual Quotation? Quotation { get; set; }
    }

    public class SalesInvoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string? PatientName { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public string Status { get; set; } = "Unpaid"; // Unpaid, PartiallyPaid, Paid, Overdue

        public string? PaymentMethod { get; set; }

        [StringLength(200)]
        public string? PaymentReference { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();
        public virtual ICollection<SalesInvoicePayment> Payments { get; set; } = new List<SalesInvoicePayment>();
    }

    public class SalesInvoiceItem
    {
        public int Id { get; set; }

        public int SalesInvoiceId { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public virtual SalesInvoice SalesInvoice { get; set; } = null!;
    }

    public class SalesInvoicePayment
    {
        public int Id { get; set; }

        public int SalesInvoiceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; }

        [StringLength(200)]
        public string? Reference { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public virtual SalesInvoice SalesInvoice { get; set; } = null!;
    }

    public class SalesCreditNote
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CreditNoteNumber { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string? PatientName { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        public int? OriginalSalesInvoiceId { get; set; }

        [StringLength(1000)]
        public string? Reason { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Processed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SalesInvoice? OriginalSalesInvoice { get; set; }
        public virtual ICollection<SalesCreditNoteItem> SalesCreditNoteItems { get; set; } = new List<SalesCreditNoteItem>();
    }

    public class SalesCreditNoteItem
    {
        public int Id { get; set; }

        public int SalesCreditNoteId { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public virtual SalesCreditNote SalesCreditNote { get; set; } = null!;
    }

    public class InsuranceClaim
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ClaimNumber { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string? PatientName { get; set; }

        [StringLength(200)]
        public string? InsuranceProvider { get; set; }

        [StringLength(100)]
        public string? PolicyNumber { get; set; }

        public int? SalesInvoiceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClaimAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovedAmount { get; set; }

        public string Status { get; set; } = "Submitted"; // Submitted, UnderReview, Approved, Rejected, Processed

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SalesInvoice? SalesInvoice { get; set; }
    }
}
