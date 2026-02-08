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
        public int CustomerId { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Tax { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }
        
        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal CashReceived { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Change { get; set; }
        
        public string Status { get; set; } = "Completed";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
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
}
