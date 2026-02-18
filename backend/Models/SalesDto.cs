using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models
{
    // Sales DTOs
    public class SaleDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentDetails { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SaleDetailDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentDetails { get; set; } = string.Empty;
        public decimal CashReceived { get; set; }
        public decimal Change { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RefundReason { get; set; } = string.Empty;
        public DateTime? RefundedAt { get; set; }
        public List<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
    }

    public class SaleItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class RefundRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class SalesReportDto
    {
        public string Period { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransaction { get; set; }
        public double MonthlyGrowth { get; set; }
        public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, int> SalesByStatus { get; set; } = new Dictionary<string, int>();
    }
}
