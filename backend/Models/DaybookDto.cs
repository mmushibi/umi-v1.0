using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models
{
    // Daybook specific DTOs for Cashier Portal
    public class DaybookTransactionDto
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<DaybookItemDto> Items { get; set; } = new List<DaybookItemDto>();
    }

    public class DaybookItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class DaybookSummaryDto
    {
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageTransaction { get; set; }
        public int CompletedSales { get; set; }
        public int PendingSales { get; set; }
        public int RefundedSales { get; set; }
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, int> SalesByStatus { get; set; } = new Dictionary<string, int>();
    }

    public class DaybookFilterDto
    {
        public string SearchQuery { get; set; } = "";
        public string DateRange { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string Status { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class DaybookReportRequestDto
    {
        public string Period { get; set; } = "today";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Format { get; set; } = "pdf"; // pdf or csv
        public bool IncludeSummary { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
    }

    public class DaybookReportDto
    {
        public string Period { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public DaybookSummaryDto Summary { get; set; } = new DaybookSummaryDto();
        public List<DaybookTransactionDto> Transactions { get; set; } = new List<DaybookTransactionDto>();
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
    }
}
