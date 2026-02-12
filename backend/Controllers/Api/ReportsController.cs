using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IO;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;
using UmiHealthPOS.Services;
using System.Security.Claims;
using System.Security.Principal;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly ReportsService _reportsService;

        public ReportsController(ApplicationDbContext context, ReportsService reportsService, ILogger<ReportsController> logger, IInventoryService inventoryService, IPrescriptionService prescriptionService)
        {
            _context = context;
            _reportsService = reportsService;
            _logger = logger;
            _inventoryService = inventoryService;
            _prescriptionService = prescriptionService;
        }

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateReport([FromQuery] string reportType, [FromQuery] string dateRange, [FromQuery] string branch = "all", [FromQuery] string startDate = "", [FromQuery] string endDate = "")
        {
            string? userId = null;
            try
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Cashier";

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var (start, end) = GetDateRange(dateRange, startDate, endDate);

                var reportData = reportType.ToLower() switch
                {
                    "sales" => await _reportsService.GenerateSalesReportAsync(start, end, branch, new List<string> { "all" }),
                    "inventory" => await _reportsService.GenerateInventoryReportAsync(start, end, branch, new List<string> { "all" }),
                    "prescriptions" => await _reportsService.GeneratePrescriptionsReportAsync(start, end, branch, new List<string> { "all" }),
                    "financial" => await _reportsService.GenerateFinancialReportAsync(start, end, branch, new List<string> { "all" }),
                    "patients" => await _reportsService.GeneratePatientsReportAsync(start, end, branch, new List<string> { "all" }),
                    "staff" => await _reportsService.GenerateStaffReportAsync(start, end, branch, new List<string> { "all" }),
                    _ => throw new ArgumentException($"Unsupported report type: {reportType}")
                };

                return Ok(reportData);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to report {ReportType} by user {UserId}", reportType, userId ?? "unknown");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {ReportType} report", reportType);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportReport(
            [FromQuery] string reportType = "sales",
            [FromQuery] string dateRange = "month",
            [FromQuery] string branch = "all",
            [FromQuery] string format = "excel",
            [FromQuery] string startDate = "",
            [FromQuery] string endDate = "")
        {
            try
            {
                var (start, end) = GetDateRange(dateRange, startDate, endDate);

                var reportData = reportType.ToLower() switch
                {
                    "sales" => await GenerateSalesReport(start, end, branch),
                    "inventory" => await GenerateInventoryReport(start, end, branch),
                    "prescriptions" => await GeneratePrescriptionsReport(start, end, branch),
                    "financial" => await GenerateFinancialReport(start, end, branch),
                    "patients" => await GeneratePatientsReport(start, end, branch),
                    "staff" => await GenerateStaffReport(start, end, branch),
                    _ => throw new ArgumentException($"Unsupported report type: {reportType}")
                };

                var fileName = $"{reportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

                return format.ToLower() switch
                {
                    "excel" => await ExportToExcel(reportData, fileName),
                    "csv" => await ExportToCsv(reportData, fileName),
                    "pdf" => await ExportToPdf(reportData, fileName),
                    _ => BadRequest(new { error = "Unsupported export format" })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting {ReportType} report", reportType);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("schedule")]
        public ActionResult ScheduleReport([FromBody] ScheduleReportRequest request)
        {
            try
            {
                // TODO: Implement report scheduling with database storage
                _logger.LogInformation("Report scheduling requested: {ReportType}, Frequency: {Frequency}",
                    request.ReportType, request.Frequency);

                return Ok(new
                {
                    success = true,
                    message = "Report scheduled successfully",
                    scheduleId = Guid.NewGuid().ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Cashier";

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var branches = await _reportsService.GetUserBranchesAsync(userId, userRole);

                var branchInfo = branches.Select(b => new BranchInfo
                {
                    Id = b.Id.ToString(),
                    Name = b.Name
                }).ToList();

                // Add "All Branches" option at the beginning if user has access to multiple branches
                if (branchInfo.Count > 1)
                {
                    branchInfo.Insert(0, new BranchInfo { Id = "all", Name = "All Branches" });
                }
                else if (branchInfo.Count == 0)
                {
                    // Fallback if no branches assigned
                    branchInfo.Add(new BranchInfo { Id = "all", Name = "All Branches" });
                }

                return Ok(branchInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches for user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private async Task<ReportData> GenerateSalesReport(DateTime start, DateTime end, string branch)
        {
            var salesQuery = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end);

            // Apply branch filter if specified
            if (branch != "all")
            {
                // TODO: Implement branch filtering when branch data is available
            }

            var sales = await salesQuery.ToListAsync();

            var completedSales = sales.Where(s => s.Status == "completed").ToList();
            var totalRevenue = completedSales.Sum(s => s.Total);
            var totalTransactions = completedSales.Count;
            var avgTransactionValue = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

            // Get previous period data for growth calculations
            var previousPeriodStart = start.AddDays(-(end - start).Days);
            var previousSales = await _context.Sales
                .Where(s => s.CreatedAt >= previousPeriodStart && s.CreatedAt < start)
                .ToListAsync();

            var previousRevenue = previousSales.Where(s => s.Status == "completed").Sum(s => s.Total);
            var revenueGrowth = previousRevenue > 0 ?
                ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;

            // Top products
            var topProducts = sales
                .Where(s => s.Status == "completed")
                .SelectMany(s => s.SaleItems)
                .GroupBy(si => si.Product.Name)
                .Select(g => new TopProductData
                {
                    Name = g.Key,
                    UnitsSold = g.Sum(si => si.Quantity),
                    Revenue = g.Sum(si => si.TotalPrice)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            // Sales by payment method
            var salesByPaymentMethod = completedSales
                .GroupBy(s => s.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Total));

            // Daily sales data for charts
            var dailySales = completedSales
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new DailyData
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(s => s.Total),
                    Transactions = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["totalRevenue"] = totalRevenue,
                    ["revenueGrowth"] = Math.Round(revenueGrowth, 2),
                    ["totalSales"] = totalTransactions,
                    ["avgOrderValue"] = avgTransactionValue,
                    ["newCustomers"] = sales.Where(s => s.Customer != null).Select(s => s.CustomerId).Distinct().Count()
                },
                TopProducts = topProducts.Select(p => new TopProduct
                {
                    Name = p.Name,
                    QuantitySold = p.UnitsSold,
                    Revenue = p.Revenue
                }).ToList(),
                Charts = new Dictionary<string, object>
                {
                    ["revenue"] = dailySales.Select(d => new { date = d.Date, value = d.Revenue }).ToList(),
                    ["transactions"] = dailySales.Select(d => new { date = d.Date, value = d.Transactions }).ToList(),
                    ["paymentMethods"] = salesByPaymentMethod
                }
            };
        }

        private async Task<ReportData> GenerateInventoryReport(DateTime start, DateTime end, string branch)
        {
            var inventoryItems = await _inventoryService.GetInventoryItemsAsync();

            // Apply branch filter if specified
            if (branch != "all")
            {
                // TODO: Implement branch filtering
            }

            var totalItems = inventoryItems.Count;
            var lowStockItems = inventoryItems.Where(i => i.Quantity <= i.ReorderLevel).Count();
            var totalValue = inventoryItems.Sum(i => i.Quantity * i.UnitPrice);

            // Inventory by category
            var inventoryByCategory = inventoryItems
                .GroupBy(i => i.PackingType ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity * i.UnitPrice));

            // Low stock items
            var lowStockDetails = inventoryItems
                .Where(i => i.Quantity <= i.ReorderLevel)
                .Select(i => new ReportLowStockItem
                {
                    Name = i.InventoryItemName,
                    CurrentStock = i.Quantity,
                    ReorderLevel = i.ReorderLevel,
                    LastUpdated = i.UpdatedAt
                })
                .ToList();

            return new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["totalItems"] = totalItems,
                    ["lowStockItems"] = lowStockItems,
                    ["totalValue"] = totalValue,
                    ["stockTurnover"] = 0 // TODO: Calculate turnover
                },
                Charts = new Dictionary<string, object>
                {
                    ["categories"] = inventoryByCategory,
                    ["lowStock"] = lowStockDetails
                }
            };
        }

        private async Task<ReportData> GeneratePrescriptionsReport(DateTime start, DateTime end, string branch)
        {
            var prescriptions = await _prescriptionService.GetPrescriptionsAsync();
            var filteredPrescriptions = prescriptions
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
                .ToList();

            var totalPrescriptions = filteredPrescriptions.Count;
            var pendingPrescriptions = filteredPrescriptions.Count(p => p.Status == "pending");
            var completedPrescriptions = filteredPrescriptions.Count(p => p.Status == "filled");
            var totalPrescriptionValue = filteredPrescriptions.Sum(p => p.TotalCost);

            // Prescriptions by status
            var prescriptionsByStatus = filteredPrescriptions
                .GroupBy(p => p.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            // Top medications
            var topMedications = filteredPrescriptions
                .GroupBy(p => p.Medication)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(m => m.Count)
                .Take(10)
                .ToList();

            return new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["totalPrescriptions"] = totalPrescriptions,
                    ["pendingPrescriptions"] = pendingPrescriptions,
                    ["completedPrescriptions"] = completedPrescriptions,
                    ["totalValue"] = totalPrescriptionValue
                },
                Charts = new Dictionary<string, object>
                {
                    ["status"] = prescriptionsByStatus,
                    ["medications"] = topMedications
                }
            };
        }

        private async Task<ReportData> GenerateFinancialReport(DateTime start, DateTime end, string branch)
        {
            var sales = await _context.Sales
                .Where(s => s.CreatedAt >= start && s.CreatedAt <= end && s.Status == "completed")
                .ToListAsync();

            var revenue = sales.Sum(s => s.Total);
            var taxCollected = sales.Sum(s => s.Tax);
            var netRevenue = revenue - taxCollected;

            // Daily revenue
            var dailyRevenue = sales
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new DailyData
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(s => s.Total),
                    Transactions = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["revenue"] = revenue,
                    ["taxCollected"] = taxCollected,
                    ["netRevenue"] = netRevenue,
                    ["transactions"] = sales.Count
                },
                Charts = new Dictionary<string, object>
                {
                    ["revenue"] = dailyRevenue.Select(d => new { date = d.Date, value = d.Revenue }).ToList()
                }
            };
        }

        private Task<ReportData> GeneratePatientsReport(DateTime start, DateTime end, string branch)
        {
            // TODO: Implement with actual data when patient service is available
            var reportData = new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["activePatients"] = 0,
                    ["newPatients"] = 0,
                    ["totalPrescriptions"] = 0
                }
            };
            return Task.FromResult(reportData);
        }

        private Task<ReportData> GenerateStaffReport(DateTime start, DateTime end, string branch)
        {
            // TODO: Implement staff performance metrics when user management is available
            var reportData = new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["totalStaff"] = 0,
                    ["activeStaff"] = 0,
                    ["avgPerformance"] = 0
                }
            };
            return Task.FromResult(reportData);
        }

        private (DateTime start, DateTime end) GetDateRange(string dateRange, string startDate, string endDate)
        {
            var now = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var customStart))
            {
                var customEnd = !string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end)
                    ? end
                    : now;
                return (customStart, customEnd);
            }

            return dateRange.ToLower() switch
            {
                "today" => (now.Date, now.Date.AddDays(1)),
                "week" => (now.Date.AddDays(-7), now),
                "month" => (new DateTime(now.Year, now.Month, 1), now),
                "quarter" => (new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1), now),
                "year" => (new DateTime(now.Year, 1, 1), now),
                _ => (new DateTime(now.Year, now.Month, 1), now)
            };
        }

        private Task<IActionResult> ExportToExcel(ReportData data, string fileName)
        {
            var csv = new StringBuilder();

            // Add metrics
            csv.AppendLine("Report Metrics");
            csv.AppendLine("Metric,Value");
            foreach (var metric in data.Metrics)
            {
                csv.AppendLine($"{metric.Key},{metric.Value}");
            }
            csv.AppendLine();

            // Add top products if available
            if (data.TopProducts?.Count > 0)
            {
                csv.AppendLine("Top Products");
                csv.AppendLine("Product,Quantity Sold,Revenue");
                foreach (var product in data.TopProducts)
                {
                    csv.AppendLine($"{product.Name},{product.QuantitySold},{product.Revenue}");
                }
                csv.AppendLine();
            }

            var content = Encoding.UTF8.GetBytes(csv.ToString());
            fileName += ".csv";

            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Task.FromResult((IActionResult)File(content, "text/csv", fileName));
        }

        private Task<IActionResult> ExportToCsv(ReportData data, string fileName)
        {
            return ExportToExcel(data, fileName); // Same implementation for now
        }

        private Task<IActionResult> ExportToPdf(ReportData data, string fileName)
        {
            // TODO: Implement PDF export
            // For now, return CSV as fallback
            return ExportToExcel(data, fileName);
        }
    }

    // Data Models
    public class ReportData
    {
        public string ReportType { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<TopProduct> TopProducts { get; set; } = new();
        public Dictionary<string, object> Charts { get; set; } = new();
    }

    public class TopProductData
    {
        public string Name { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailyData
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Transactions { get; set; }
    }

    public class ReportLowStockItem
    {
        public string Name { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class BranchInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ScheduleReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
    }
}
