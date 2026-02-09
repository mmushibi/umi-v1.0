using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models
{
    // Sales DTOs
    public class SaleDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; }
        public string DateTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerId { get; set; }
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentDetails { get; set; }
        public string Status { get; set; }
    }

    public class SaleDetailDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; }
        public string DateTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentDetails { get; set; }
        public decimal CashReceived { get; set; }
        public decimal Change { get; set; }
        public string Status { get; set; }
        public string RefundReason { get; set; }
        public string RefundedAt { get; set; }
        public List<SaleItemDto> Items { get; set; }
    }

    public class SaleItemDto
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class RefundRequest
    {
        public string Reason { get; set; }
    }

    public class SalesReportDto
    {
        public string Period { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransaction { get; set; }
        public double MonthlyGrowth { get; set; }
        public Dictionary<string, decimal> SalesByPaymentMethod { get; set; }
        public Dictionary<string, int> SalesByStatus { get; set; }
    }
}
