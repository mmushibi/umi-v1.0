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
using UmiHealthPOS.DTOs;
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

                int? branchId = null;
                if (branch != "all")
                {
                    if (int.TryParse(branch, out var parsedBranchId))
                    {
                        branchId = parsedBranchId;
                    }
                }

                object reportData = reportType.ToLower() switch
                {
                    "sales" => await _reportsService.GetSalesReportsAsync(userId, userRole, start, end, branchId),
                    "inventory" => await _reportsService.GetInventoryReportsAsync(userId, userRole, branchId),
                    "financial" => await _reportsService.GetFinancialReportsAsync(userId, userRole, start, end, branchId),
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
        public async Task<ActionResult> ScheduleReport([FromBody] ScheduleReportRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tenantIdClaim = User.FindFirst("TenantId")?.Value;
                
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantIdClaim))
                {
                    return Unauthorized("User not authenticated");
                }

                // Calculate next run date based on frequency
                var nextRunDate = ReportHelper.CalculateNextRunDate(request.Frequency, request.StartDate ?? DateTime.UtcNow);

                var schedule = new ReportSchedule
                {
                    ReportType = request.ReportType,
                    Frequency = request.Frequency,
                    TenantId = tenantIdClaim,
                    BranchId = request.BranchId == "all" ? null : int.Parse(request.BranchId),
                    UserId = userId,
                    RecipientEmail = request.RecipientEmail,
                    NextRunDate = nextRunDate,
                    Parameters = System.Text.Json.JsonSerializer.Serialize(request.Parameters),
                    IsActive = true
                };

                _context.ReportSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Report scheduled successfully: {ReportType}, ScheduleId: {ScheduleId}",
                    request.ReportType, schedule.Id);

                return Ok(new
                {
                    success = true,
                    message = "Report scheduled successfully",
                    scheduleId = schedule.Id,
                    nextRunDate = nextRunDate
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
                if (int.TryParse(branch, out var branchId))
                {
                    salesQuery = salesQuery.Where(s => s.BranchId == branchId);
                }
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
                if (int.TryParse(branch, out var branchId))
                {
                    inventoryItems = inventoryItems.Where(ii => ii.BranchId == branchId).ToList();
                }
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
                    ["stockTurnover"] = ReportHelper.CalculateStockTurnover(inventoryItems)
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

        private async Task<ReportData> GeneratePatientsReport(DateTime start, DateTime end, string branch)
        {
            var patients = await _context.Patients
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
                .ToListAsync();

            // Apply branch filter if specified
            if (branch != "all" && int.TryParse(branch, out var branchId))
            {
                patients = patients.Where(p => p.BranchId == branchId).ToList();
            }

            var totalPatients = patients.Count;
            var newPatients = patients.Count(p => p.CreatedAt >= start && p.CreatedAt <= end);
            var activePatients = patients.Count(p => p.IsActive);

            return new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["totalPatients"] = totalPatients,
                    ["newPatients"] = newPatients,
                    ["activePatients"] = activePatients
                }
            };
        }

        private async Task<ReportData> GenerateStaffReport(DateTime start, DateTime end, string branch)
        {
            var employees = await _context.Employees
                .Where(e => e.CreatedAt >= start && e.CreatedAt <= end)
                .ToListAsync();

            // Apply branch filter if specified
            if (branch != "all" && int.TryParse(branch, out var branchId))
            {
                employees = employees.Where(e => e.BranchId == branchId).ToList();
            }

            var totalStaff = employees.Count;
            var activeStaff = employees.Count(e => e.IsActive);
            var newStaff = employees.Count(e => e.CreatedAt >= start && e.CreatedAt <= end);
            var staffByRole = employees.GroupBy(e => e.Role)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ReportData
            {
                Metrics = new Dictionary<string, object>
                {
                    ["totalStaff"] = totalStaff,
                    ["activeStaff"] = activeStaff,
                    ["newStaff"] = newStaff,
                    ["staffByRole"] = staffByRole
                }
            };
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

        private async Task<IActionResult> ExportToPdf(ReportData data, string fileName)
        {
            try
            {
                // Generate HTML content for PDF
                var htmlContent = ReportHelper.GeneratePdfHtmlContent(data, fileName);
                
                // Convert HTML to PDF using a library like PuppeteerSharp or iTextSharp
                // For now, we'll return the HTML as a file that can be printed to PDF
                var bytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
                
                return await Task.FromResult(File(bytes, "text/html", $"{fileName}.html"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF export");
                // Fallback to Excel export
                return await ExportToExcel(data, fileName);
            }
        }
    }

    // Data Models
    public class TopProduct
    {
        public string Name { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

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
        public string BranchId { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    // Helper Methods
    public static class ReportHelper
    {
        public static DateTime CalculateNextRunDate(string frequency, DateTime startDate)
        {
            return frequency.ToLower() switch
            {
                "daily" => startDate.AddDays(1),
                "weekly" => startDate.AddDays(7),
                "monthly" => startDate.AddMonths(1),
                "quarterly" => startDate.AddMonths(3),
                "yearly" => startDate.AddYears(1),
                _ => startDate.AddDays(1)
            };
        }

        public static double CalculateStockTurnover(IEnumerable<InventoryItem> items)
        {
            // Simple turnover calculation: (Cost of Goods Sold) / Average Inventory
            // For now, we'll use a simplified calculation based on quantity and price
            var totalValue = items.Sum(i => i.Quantity * i.UnitPrice);
            var totalItems = items.Sum(i => i.Quantity);
            
            if (totalItems == 0) return 0;
            
            // This is a simplified calculation - in a real implementation,
            // you'd track actual sales data over time
            return Math.Round((double)totalValue / totalItems, 2);
        }

        public static string GeneratePdfHtmlContent(ReportData data, string fileName)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{fileName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #333; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .metric {{ margin: 10px 0; }}
        .metric-label {{ font-weight: bold; }}
    </style>
</head>
<body>
    <h1>{fileName}</h1>
    <p>Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>
    
    <h2>Metrics</h2>";

            foreach (var metric in data.Metrics)
            {
                html += $@"
    <div class=""metric"">
        <span class=""metric-label"">{metric.Key}:</span> {metric.Value}
    </div>";
            }

            if (data.TopProducts?.Count > 0)
            {
                html += @"
    <h2>Top Products</h2>
    <table>
        <tr><th>Product</th><th>Quantity Sold</th><th>Revenue</th></tr>";

                foreach (var product in data.TopProducts)
                {
                    html += $@"
        <tr><td>{product.Name}</td><td>{product.QuantitySold}</td><td>{product.Revenue:C}</td></tr>";
                }

                html += @"
    </table>";
            }

            html += @"
</body>
</html>";

            return html;
        }
    }
}


