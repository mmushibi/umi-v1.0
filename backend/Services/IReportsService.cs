using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface IReportsService
    {
        Task<List<Branch>> GetUserBranchesAsync(string userId, string userRole);
        Task<List<Models.SalesReportDto>> GetSalesReportsAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate, int? branchId = null);
        Task<List<InventoryReportDto>> GetInventoryReportsAsync(string userId, string userRole, int? branchId = null);
        Task<List<FinancialReportDto>> GetFinancialReportsAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate, int? branchId = null);
        Task<byte[]> ExportReportAsync(string reportType, string format, object parameters);
        Task<List<BranchPerformanceDto>> GetBranchPerformanceAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate);
    }

    // Supporting DTOs for Reports (only those not in Models)
    public class InventoryReportDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }

    public class FinancialReportDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    public class BranchPerformanceDto
    {
        public int Id { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalSales { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public int UniqueCustomers { get; set; }
        public decimal GrowthPercentage { get; set; }
    }
}
