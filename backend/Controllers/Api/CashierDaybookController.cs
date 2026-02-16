using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Filters;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(PerformanceMonitoringFilter))]
    [Authorize(Roles = "Cashier,Pharmacist,TenantAdmin")] // Allow access to all roles that can view daybook
    public class CashierDaybookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CashierDaybookController> _logger;

        public CashierDaybookController(
            ApplicationDbContext context,
            ILogger<CashierDaybookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            // TODO: Implement proper JWT token extraction
            return User?.Identity?.Name ?? "demo-user";
        }

        private string GetCurrentTenantId()
        {
            // TODO: Implement proper tenant extraction from JWT
            return "demo-tenant";
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<object>> GetTransactions([FromQuery] DaybookFilterDto filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var query = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .Where(s => s.TenantId == tenantId)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchQuery))
                {
                    query = query.Where(s =>
                        s.ReceiptNumber.Contains(filter.SearchQuery) ||
                        (s.Customer != null && s.Customer.Name.Contains(filter.SearchQuery)));
                }

                if (!string.IsNullOrEmpty(filter.PaymentMethod))
                {
                    query = query.Where(s => s.PaymentMethod == filter.PaymentMethod);
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(s => s.Status == filter.Status);
                }

                // Apply date range filter
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(s => s.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    var endDate = filter.EndDate.Value.AddDays(1); // Include the entire end date
                    query = query.Where(s => s.CreatedAt < endDate);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var transactions = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(s => new DaybookTransactionDto
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        CreatedAt = s.CreatedAt,
                        CustomerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                        ItemCount = s.SaleItems.Count,
                        Subtotal = s.Subtotal,
                        Tax = s.Tax,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        Status = s.Status,
                        Items = s.SaleItems.Select(si => new DaybookItemDto
                        {
                            ProductName = si.Product.Name,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            TotalPrice = si.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    transactions,
                    pagination = new
                    {
                        currentPage = filter.Page,
                        pageSize = filter.PageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daybook transactions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DaybookSummaryDto>> GetSummary([FromQuery] DaybookFilterDto filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var query = _context.Sales
                    .Include(s => s.Customer)
                    .Where(s => s.TenantId == tenantId)
                    .AsQueryable();

                // Apply same filters as transactions
                if (!string.IsNullOrEmpty(filter.SearchQuery))
                {
                    query = query.Where(s =>
                        s.ReceiptNumber.Contains(filter.SearchQuery) ||
                        (s.Customer != null && s.Customer.Name.Contains(filter.SearchQuery)));
                }

                if (!string.IsNullOrEmpty(filter.PaymentMethod))
                {
                    query = query.Where(s => s.PaymentMethod == filter.PaymentMethod);
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(s => s.Status == filter.Status);
                }

                // Apply date range filter
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(s => s.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    var endDate = filter.EndDate.Value.AddDays(1);
                    query = query.Where(s => s.CreatedAt < endDate);
                }

                var sales = await query.ToListAsync();

                var completedSales = sales.Where(s => s.Status == "completed").ToList();
                var totalRevenue = completedSales.Sum(s => s.Total);
                var totalTransactions = sales.Count;
                var averageTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

                var summary = new DaybookSummaryDto
                {
                    TotalTransactions = totalTransactions,
                    TotalRevenue = totalRevenue,
                    AverageTransaction = averageTransaction,
                    CompletedSales = completedSales.Count,
                    PendingSales = sales.Count(s => s.Status == "pending"),
                    RefundedSales = sales.Count(s => s.Status == "refunded"),
                    RevenueByPaymentMethod = completedSales
                        .GroupBy(s => s.PaymentMethod)
                        .ToDictionary(g => g.Key, g => g.Sum(s => s.Total)),
                    SalesByStatus = sales
                        .GroupBy(s => s.Status)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daybook summary");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("transaction/{id}")]
        public async Task<ActionResult<DaybookTransactionDto>> GetTransaction(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var transaction = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .Where(s => s.TenantId == tenantId && s.Id == id)
                    .Select(s => new DaybookTransactionDto
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        CreatedAt = s.CreatedAt,
                        CustomerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                        ItemCount = s.SaleItems.Count,
                        Subtotal = s.Subtotal,
                        Tax = s.Tax,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        Status = s.Status,
                        Items = s.SaleItems.Select(si => new DaybookItemDto
                        {
                            ProductName = si.Product.Name,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            TotalPrice = si.TotalPrice
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    return NotFound(new { error = "Transaction not found" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction details");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("report")]
        public async Task<ActionResult<DaybookReportDto>> GenerateReport([FromBody] DaybookReportRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Determine date range
                var (startDate, endDate) = request.Period.ToLower() switch
                {
                    "today" => (DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1)),
                    "yesterday" => (DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date),
                    "thisweek" => (DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek), DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek)),
                    "lastweek" => (DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek - 7), DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek)),
                    "thismonth" => (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1), new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month + 1, 1)),
                    "lastmonth" => (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month - 1, 1), new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)),
                    "custom" when request.StartDate.HasValue && request.EndDate.HasValue => (request.StartDate.Value, request.EndDate.Value.AddDays(1)),
                    _ => (DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1))
                };

                if (request.StartDate.HasValue)
                {
                    startDate = request.StartDate.Value;
                }

                if (request.EndDate.HasValue)
                {
                    endDate = request.EndDate.Value.AddDays(1);
                }

                var query = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate && s.CreatedAt < endDate)
                    .AsQueryable();

                var transactions = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new DaybookTransactionDto
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        CreatedAt = s.CreatedAt,
                        CustomerName = s.Customer != null ? s.Customer.Name : "Walk-in Customer",
                        ItemCount = s.SaleItems.Count,
                        Subtotal = s.Subtotal,
                        Tax = s.Tax,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        Status = s.Status,
                        Items = s.SaleItems.Select(si => new DaybookItemDto
                        {
                            ProductName = si.Product.Name,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            TotalPrice = si.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync();

                var completedSales = transactions.Where(t => t.Status == "completed").ToList();
                var totalRevenue = completedSales.Sum(t => t.Total);
                var totalTransactions = transactions.Count;
                var averageTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

                var summary = new DaybookSummaryDto
                {
                    TotalTransactions = totalTransactions,
                    TotalRevenue = totalRevenue,
                    AverageTransaction = averageTransaction,
                    CompletedSales = completedSales.Count,
                    PendingSales = transactions.Count(t => t.Status == "pending"),
                    RefundedSales = transactions.Count(t => t.Status == "refunded"),
                    RevenueByPaymentMethod = completedSales
                        .GroupBy(t => t.PaymentMethod)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.Total)),
                    SalesByStatus = transactions
                        .GroupBy(t => t.Status)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                var report = new DaybookReportDto
                {
                    Period = request.Period,
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.AddDays(-1).ToString("yyyy-MM-dd"),
                    Summary = request.IncludeSummary ? summary : new DaybookSummaryDto(),
                    Transactions = request.IncludeDetails ? transactions : new List<DaybookTransactionDto>(),
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedBy = userId
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daybook report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv([FromQuery] DaybookReportRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Generate report data
                var reportResult = await GenerateReport(request);
                if (reportResult.Result is not OkObjectResult okResult || okResult.Value is not DaybookReportDto report)
                {
                    return BadRequest(new { error = "Failed to generate report" });
                }

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Receipt Number,Date,Customer,Items,Subtotal,Tax,Total,Payment Method,Status");

                foreach (var transaction in report.Transactions)
                {
                    var items = string.Join(";", transaction.Items.Select(i => $"{i.ProductName}({i.Quantity})"));
                    csv.AppendLine($"\"{transaction.ReceiptNumber}\"," +
                                   $"\"{transaction.CreatedAt:yyyy-MM-dd HH:mm:ss}\"," +
                                   $"\"{transaction.CustomerName}\"," +
                                   $"\"{items}\"," +
                                   $"\"{transaction.Subtotal}\"," +
                                   $"\"{transaction.Tax}\"," +
                                   $"\"{transaction.Total}\"," +
                                   $"\"{transaction.PaymentMethod}\"," +
                                   $"\"{transaction.Status}\"");
                }

                var fileName = $"daybook_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting daybook to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
