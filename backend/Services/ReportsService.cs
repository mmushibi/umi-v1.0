using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace UmiHealthPOS.Services
{
    public class ReportsService : IReportsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsService> _logger;

        public ReportsService(ApplicationDbContext context, ILogger<ReportsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Branch>> GetUserBranchesAsync(string userId, string userRole)
        {
            try
            {
                var query = _context.UserBranches
                    .Where(ub => ub.UserId == userId && ub.IsActive && ub.Branch.IsActive);

                if (userRole == "SuperAdmin")
                {
                    // Super admins can see all branches
                    return await _context.Branches
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.Name)
                        .ToListAsync();
                }

                return await query
                    .Select(ub => ub.Branch)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user branches for user: {UserId}", userId);
                return new List<Branch>();
            }
        }

        public async Task<List<Models.SalesReportDto>> GetSalesReportsAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate, int? branchId = null)
        {
            try
            {
                var query = _context.Sales.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= endDate.Value);

                if (branchId.HasValue)
                    query = query.Where(s => s.BranchId == branchId.Value);

                var sales = await query
                    .GroupBy(s => new
                    {
                        Year = s.CreatedAt.Year,
                        Month = s.CreatedAt.Month
                    })
                    .Select(g => new Models.SalesReportDto
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        StartDate = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM-dd"),
                        EndDate = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)).ToString("yyyy-MM-dd"),
                        TotalRevenue = g.Sum(s => s.Total),
                        TotalTransactions = g.Count(),
                        AverageTransaction = g.Average(s => s.Total),
                        MonthlyGrowth = 0 // Would need previous month data for calculation
                    })
                    .OrderByDescending(r => r.Period)
                    .ToListAsync();

                return sales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales reports");
                return new List<Models.SalesReportDto>();
            }
        }

        public async Task<List<InventoryReportDto>> GetInventoryReportsAsync(string userId, string userRole, int? branchId = null)
        {
            try
            {
                var query = _context.InventoryItems.AsQueryable();

                if (branchId.HasValue)
                    query = query.Where(i => i.BranchId == branchId.Value);

                var inventory = await query
                    .Select(i => new InventoryReportDto
                    {
                        Id = i.Id,
                        ProductName = i.Name,
                        CurrentStock = i.Quantity,
                        ReorderLevel = i.ReorderLevel,
                        UnitPrice = i.UnitPrice,
                        TotalValue = i.Quantity * i.UnitPrice,
                        Status = i.Quantity <= i.ReorderLevel ? "Low Stock" : "In Stock",
                        BranchName = i.Branch != null ? i.Branch.Name : "Unknown"
                    })
                    .ToListAsync();

                return inventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory reports");
                return new List<InventoryReportDto>();
            }
        }

        public async Task<List<FinancialReportDto>> GetFinancialReportsAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate, int? branchId = null)
        {
            try
            {
                var query = _context.Sales.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= endDate.Value);

                if (branchId.HasValue)
                    query = query.Where(s => s.BranchId == branchId.Value);

                var financial = await query
                    .Join(_context.Branches, s => s.BranchId, b => b.Id, (s, b) => new { s, b })
                    .GroupBy(x => new { x.s.CreatedAt.Date, Branch = x.b.Name })
                    .Select(g => new FinancialReportDto
                    {
                        Id = g.FirstOrDefault().s.Id,
                        Date = g.Key.Date,
                        Description = $"Daily Sales - {g.Key.Date:yyyy-MM-dd}",
                        Revenue = g.Sum(x => x.s.Total),
                        Expenses = 0, // Would need expense tracking
                        NetProfit = g.Sum(x => x.s.Total),
                        BranchName = g.Key.Branch
                    })
                    .ToListAsync();

                return financial;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial reports");
                return new List<FinancialReportDto>();
            }
        }

        public async Task<byte[]> ExportReportAsync(string reportType, string format, object parameters)
        {
            // Placeholder implementation for report export
            await Task.CompletedTask;
            return new byte[0];
        }

        public async Task<List<BranchPerformanceDto>> GetBranchPerformanceAsync(string userId, string userRole, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.Sales.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedAt <= endDate.Value);

                var performance = await query
                    .Join(_context.Branches, s => s.BranchId, b => b.Id, (s, b) => new { s, b })
                    .GroupBy(x => x.b)
                    .Select(g => new BranchPerformanceDto
                    {
                        Id = g.Key.Id,
                        BranchName = g.Key.Name,
                        TotalRevenue = g.Sum(x => x.s.Total),
                        TotalSales = g.Count(),
                        AverageTransactionValue = g.Average(x => x.s.Total),
                        UniqueCustomers = g.Select(x => x.s.CustomerId).Distinct().Count(),
                        GrowthPercentage = 0 // Would need historical data
                    })
                    .ToListAsync();

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch performance");
                return new List<BranchPerformanceDto>();
            }
        }
    }
}
