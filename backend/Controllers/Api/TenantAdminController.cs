using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IO;
using UmiHealthPOS.Models.Dashboard;
using UmiHealthPOS.Services;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class TenantAdminController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<TenantAdminController> _logger;
        private readonly IInventoryService _inventoryService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly ApplicationDbContext _context;

        public TenantAdminController(
            IDashboardService dashboardService,
            ILogger<TenantAdminController> logger,
            IInventoryService inventoryService,
            IPrescriptionService prescriptionService,
            ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _inventoryService = inventoryService;
            _prescriptionService = prescriptionService;
            _context = context;
        }

        [HttpGet("dashboard/stats")]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/recent-activity")]
        public async Task<ActionResult<List<RecentActivity>>> GetRecentActivity([FromQuery] int limit = 10)
        {
            try
            {
                var activities = await _dashboardService.GetRecentActivityAsync(limit);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activity");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("staff/add")]
        public async Task<ActionResult> AddStaffMember([FromBody] AddStaffRequest request)
        {
            try
            {
                // Validate request - no mock data
                // When database is implemented, this will create real staff records

                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.FirstName))
                {
                    return BadRequest(new { error = "Email and first name are required" });
                }

                _logger.LogInformation("Staff member addition requested: {Email}", request.Email);

                // Return success - no actual creation until database is implemented
                return Ok(new { success = true, message = "Staff member addition request received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing staff member addition");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("reports/summary")]
        public async Task<ActionResult<ReportSummary>> GetReportSummary([FromQuery] string period = "monthly")
        {
            try
            {
                // Return empty summary - no mock data
                // When database is implemented, this will generate real reports
                var summary = new ReportSummary
                {
                    Period = period,
                    TotalRevenue = "ZMK 0",
                    TotalSales = 0,
                    TopProducts = new List<TopProduct>()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report summary");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("inventory/products")]
        public async Task<ActionResult<List<Product>>> GetProducts()
        {
            try
            {
                var products = await _inventoryService.GetProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("sales/process")]
        public async Task<ActionResult> ProcessSale([FromBody] Services.SaleRequest request)
        {
            try
            {
                var result = await _inventoryService.ProcessSaleAsync(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        saleId = result.SaleId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sale");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("inventory/update-stock")]
        public async Task<ActionResult> UpdateStock([FromBody] StockUpdateRequest request)
        {
            try
            {
                if (request.Items == null || request.Items.Count == 0)
                {
                    return BadRequest(new { error = "No items to update" });
                }

                var success = true;
                foreach (var item in request.Items)
                {
                    var result = await _inventoryService.UpdateStockAsync(item.ProductId, item.NewStock, item.Reason);
                    if (!result)
                    {
                        success = false;
                        _logger.LogWarning("Failed to update stock for product {ProductId}", item.ProductId);
                    }
                }

                if (success)
                {
                    return Ok(new { success = true, message = "Stock updated successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, error = "Some stock updates failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("customers")]
        public async Task<ActionResult<List<Customer>>> GetCustomers()
        {
            try
            {
                var customers = await _inventoryService.GetCustomersAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("inventory/low-stock")]
        public async Task<ActionResult<List<Product>>> GetLowStockItems()
        {
            try
            {
                var lowStockItems = await _inventoryService.GetLowStockProductsAsync();
                return Ok(lowStockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock items");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("inventory/items")]
        public async Task<ActionResult<List<InventoryItem>>> GetInventoryItems()
        {
            try
            {
                var inventoryItems = await _inventoryService.GetInventoryItemsAsync();
                return Ok(inventoryItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory items");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("inventory/items")]
        public async Task<ActionResult<InventoryItem>> CreateInventoryItem([FromBody] CreateInventoryItemRequest request)
        {
            try
            {
                var inventoryItem = await _inventoryService.CreateInventoryItemAsync(request);
                return CreatedAtAction(nameof(GetInventoryItems), new { id = inventoryItem.Id }, inventoryItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory item");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("inventory/items/{id}")]
        public async Task<ActionResult<InventoryItem>> UpdateInventoryItem(int id, [FromBody] UpdateInventoryItemRequest request)
        {
            try
            {
                var inventoryItem = await _inventoryService.UpdateInventoryItemAsync(id, request);
                if (inventoryItem == null)
                {
                    return NotFound(new { error = "Inventory item not found" });
                }
                return Ok(inventoryItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory item");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("inventory/items/{id}")]
        public async Task<ActionResult> DeleteInventoryItem(int id)
        {
            try
            {
                var result = await _inventoryService.DeleteInventoryItemAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Inventory item not found" });
                }
                return Ok(new { success = true, message = "Inventory item deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory item");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("inventory/import-csv")]
        public async Task<ActionResult> ImportInventoryFromCsv(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                var result = await _inventoryService.ImportInventoryFromCsvAsync(file);
                return Ok(new
                {
                    success = true,
                    message = $"Successfully imported {result.ImportedCount} inventory items",
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing inventory from CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("inventory/export-csv")]
        public async Task<IActionResult> ExportInventoryToCsv()
        {
            try
            {
                var csvContent = await _inventoryService.ExportInventoryToCsvAsync();
                var fileName = $"inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(csvContent, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Prescription Management Endpoints
        [HttpGet("prescriptions")]
        public async Task<ActionResult<List<Prescription>>> GetPrescriptions()
        {
            try
            {
                var prescriptions = await _prescriptionService.GetPrescriptionsAsync();
                return Ok(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescriptions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("prescriptions/{id}")]
        public async Task<ActionResult<Prescription>> GetPrescription(int id)
        {
            try
            {
                var prescription = await _prescriptionService.GetPrescriptionAsync(id);
                if (prescription == null)
                {
                    return NotFound(new { error = "Prescription not found" });
                }
                return Ok(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescription with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("prescriptions")]
        public async Task<ActionResult<Prescription>> CreatePrescription([FromBody] CreatePrescriptionRequest request)
        {
            try
            {
                var prescription = await _prescriptionService.CreatePrescriptionAsync(request);
                return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("prescriptions/{id}")]
        public async Task<ActionResult<Prescription>> UpdatePrescription(int id, [FromBody] UpdatePrescriptionRequest request)
        {
            try
            {
                var prescription = await _prescriptionService.UpdatePrescriptionAsync(id, request);
                if (prescription == null)
                {
                    return NotFound(new { error = "Prescription not found" });
                }
                return Ok(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prescription with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("prescriptions/{id}")]
        public async Task<ActionResult> DeletePrescription(int id)
        {
            try
            {
                var result = await _prescriptionService.DeletePrescriptionAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Prescription not found" });
                }
                return Ok(new { success = true, message = "Prescription deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting prescription with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("prescriptions/{id}/fill")]
        public async Task<ActionResult> FillPrescription(int id)
        {
            try
            {
                var result = await _prescriptionService.FillPrescriptionAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Prescription not found" });
                }
                return Ok(new { success = true, message = "Prescription filled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filling prescription with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("prescriptions/export-csv")]
        public async Task<IActionResult> ExportPrescriptionsToCsv()
        {
            try
            {
                var csvContent = await _prescriptionService.ExportPrescriptionsToCsvAsync();
                var fileName = $"prescriptions_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(csvContent, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting prescriptions to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Patient Management Endpoints
        [HttpGet("patients")]
        public async Task<ActionResult<List<Patient>>> GetPatients()
        {
            try
            {
                var patients = await _prescriptionService.GetPatientsAsync();
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("patients/{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            try
            {
                var patient = await _prescriptionService.GetPatientAsync(id);
                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("patients")]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] CreatePatientRequest request)
        {
            try
            {
                var patient = await _prescriptionService.CreatePatientAsync(request);
                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("patients/{id}")]
        public async Task<ActionResult<Patient>> UpdatePatient(int id, [FromBody] CreatePatientRequest request)
        {
            try
            {
                var patient = await _prescriptionService.UpdatePatientAsync(id, request);
                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("patients/{id}")]
        public async Task<ActionResult> DeletePatient(int id)
        {
            try
            {
                var result = await _prescriptionService.DeletePatientAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Patient not found" });
                }
                return Ok(new { success = true, message = "Patient deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("patients/import-csv")]
        public async Task<ActionResult> ImportPatientsFromCsv(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".csv"))
                {
                    return BadRequest(new { error = "Only CSV files are allowed" });
                }

                var result = await _prescriptionService.ImportPatientsFromCsvAsync(file);
                return Ok(new { success = true, message = $"Successfully imported {result.ImportedCount} patients", errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing patients from CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("patients/export-csv")]
        public async Task<ActionResult> ExportPatientsToCsv()
        {
            try
            {
                var csvContent = await _prescriptionService.ExportPatientsToCsvAsync();
                var fileName = $"patients_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(csvContent, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting patients to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("doctors")]
        public async Task<ActionResult<List<Doctor>>> GetDoctors()
        {
            try
            {
                // In a real implementation, this would fetch from user management system
                // For now, return empty list as we don't have user management implemented
                var doctors = new List<Doctor>();

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctors");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Sales Management Endpoints
        [HttpGet("sales")]
        public async Task<ActionResult<List<SaleDto>>> GetSales(
            [FromQuery] string searchQuery = "",
            [FromQuery] string dateRange = "",
            [FromQuery] string paymentMethod = "",
            [FromQuery] string status = "")
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(s =>
                        s.ReceiptNumber.Contains(searchQuery) ||
                        (s.Customer != null && s.Customer.Name.Contains(searchQuery)));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query = query.Where(s => s.PaymentMethod == paymentMethod);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.Status == status);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateRange))
                {
                    var today = DateTime.UtcNow.Date;
                    var startDate = dateRange.ToLower() switch
                    {
                        "today" => today,
                        "week" => today.AddDays(-7),
                        "month" => new DateTime(today.Year, today.Month, 1),
                        "quarter" => new DateTime(today.Year, (today.Month / 3) * 3 + 1, 1),
                        "year" => new DateTime(today.Year, 1, 1),
                        _ => (DateTime?)null
                    };

                    if (startDate.HasValue)
                    {
                        query = query.Where(s => s.CreatedAt >= startDate.Value);
                    }
                }

                var sales = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new SaleDto
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        DateTime = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        CustomerName = s.Customer != null ? s.Customer.Name : "Walk-in",
                        CustomerId = s.CustomerId.HasValue ? s.CustomerId.Value.ToString() : null,
                        ItemCount = s.SaleItems.Count,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        PaymentDetails = s.PaymentDetails,
                        Status = s.Status
                    })
                    .ToListAsync();

                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("sales/{id}")]
        public async Task<ActionResult<SaleDetailDto>> GetSale(int id)
        {
            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sale == null)
                {
                    return NotFound(new { error = "Sale not found" });
                }

                var saleDto = new SaleDetailDto
                {
                    Id = sale.Id,
                    ReceiptNumber = sale.ReceiptNumber,
                    DateTime = sale.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    CustomerName = sale.Customer != null ? sale.Customer.Name : "Walk-in",
                    CustomerId = sale.CustomerId.HasValue ? sale.CustomerId.Value.ToString() : null,
                    Subtotal = sale.Subtotal,
                    Tax = sale.Tax,
                    Total = sale.Total,
                    PaymentMethod = sale.PaymentMethod,
                    PaymentDetails = sale.PaymentDetails,
                    CashReceived = sale.CashReceived,
                    Change = sale.Change,
                    Status = sale.Status,
                    RefundReason = sale.RefundReason,
                    RefundedAt = sale.RefundedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Items = sale.SaleItems.Select(si => new SaleItemDto
                    {
                        ProductName = si.Product.Name,
                        Quantity = si.Quantity,
                        UnitPrice = si.UnitPrice,
                        TotalPrice = si.TotalPrice
                    }).ToList()
                };

                return Ok(saleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("sales/{id}/refund")]
        public async Task<ActionResult> RefundSale(int id, [FromBody] RefundRequest request)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(id);
                if (sale == null)
                {
                    return NotFound(new { error = "Sale not found" });
                }

                if (sale.Status != "completed")
                {
                    return BadRequest(new { error = "Only completed sales can be refunded" });
                }

                sale.Status = "refunded";
                sale.RefundReason = request.Reason;
                sale.RefundedAt = DateTime.UtcNow;
                sale.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Sale refunded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding sale with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("sales/{id}")]
        public async Task<ActionResult> DeleteSale(int id)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(id);
                if (sale == null)
                {
                    return NotFound(new { error = "Sale not found" });
                }

                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Sale deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sale with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("sales/export-csv")]
        public async Task<IActionResult> ExportSalesToCsv(
            [FromQuery] string searchQuery = "",
            [FromQuery] string dateRange = "",
            [FromQuery] string paymentMethod = "",
            [FromQuery] string status = "")
        {
            try
            {
                // Get sales data directly using the same logic as GetSales endpoint
                var baseQuery = _context.Sales.AsQueryable();

                // Apply same filters as GetSales method
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    baseQuery = baseQuery.Where(s =>
                        s.ReceiptNumber.Contains(searchQuery) ||
                        (s.Customer != null && s.Customer.Name.Contains(searchQuery)));
                }

                if (!string.IsNullOrEmpty(dateRange))
                {
                    var today = DateTime.UtcNow.Date;
                    var startDate = dateRange.ToLower() switch
                    {
                        "today" => today,
                        "week" => today.AddDays(-7),
                        "month" => new DateTime(today.Year, today.Month, 1),
                        "quarter" => new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1),
                        "year" => new DateTime(today.Year, 1, 1),
                        _ => DateTime.MinValue
                    };

                    if (startDate != DateTime.MinValue)
                    {
                        baseQuery = baseQuery.Where(s => s.CreatedAt >= startDate);
                    }
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    baseQuery = baseQuery.Where(s => s.PaymentMethod == paymentMethod);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    baseQuery = baseQuery.Where(s => s.Status == status);
                }

                var sales = await baseQuery
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new SaleDto
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        DateTime = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        CustomerName = s.Customer != null ? s.Customer.Name : "Walk-in",
                        CustomerId = s.CustomerId.HasValue ? s.CustomerId.Value.ToString() : null,
                        ItemCount = s.SaleItems.Count,
                        Total = s.Total,
                        PaymentMethod = s.PaymentMethod,
                        PaymentDetails = s.PaymentDetails,
                        Status = s.Status
                    })
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("Receipt Number,Date Time,Customer,Items,Total,Payment Method,Status");

                foreach (var sale in sales)
                {
                    csv.AppendLine($"{sale.ReceiptNumber},{sale.DateTime},{sale.CustomerName},{sale.ItemCount},{sale.Total},{sale.PaymentMethod},{sale.Status}");
                }

                var fileName = $"sales_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("sales/report")]
        public async Task<ActionResult<SalesReportDto>> GetSalesReport(
            [FromQuery] string period = "month",
            [FromQuery] int year = 0,
            [FromQuery] int month = 0)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startDate = period.ToLower() switch
                {
                    "today" => now.Date,
                    "week" => now.Date.AddDays(-7),
                    "month" => new DateTime(year > 0 ? year : now.Year, month > 0 ? month : now.Month, 1),
                    "quarter" => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
                    "year" => new DateTime(year > 0 ? year : now.Year, 1, 1),
                    _ => new DateTime(now.Year, now.Month, 1)
                };

                var endDate = period.ToLower() switch
                {
                    "today" => startDate.AddDays(1),
                    "week" => now,
                    "month" => startDate.AddMonths(1),
                    "quarter" => startDate.AddMonths(3),
                    "year" => startDate.AddYears(1),
                    _ => startDate.AddMonths(1)
                };

                var sales = await _context.Sales
                    .Where(s => s.CreatedAt >= startDate && s.CreatedAt < endDate)
                    .ToListAsync();

                var totalRevenue = sales.Where(s => s.Status == "completed").Sum(s => s.Total);
                var totalTransactions = sales.Count(s => s.Status == "completed");
                var avgTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

                var previousPeriodStart = period.ToLower() switch
                {
                    "today" => startDate.AddDays(-1),
                    "week" => startDate.AddDays(-7),
                    "month" => startDate.AddMonths(-1),
                    "quarter" => startDate.AddMonths(-3),
                    "year" => startDate.AddYears(-1),
                    _ => startDate.AddMonths(-1)
                };

                var previousPeriodEnd = startDate;

                var previousSales = await _context.Sales
                    .Where(s => s.CreatedAt >= previousPeriodStart && s.CreatedAt < previousPeriodEnd)
                    .ToListAsync();

                var previousRevenue = previousSales.Where(s => s.Status == "completed").Sum(s => s.Total);
                var monthlyGrowth = previousRevenue > 0 ?
                    ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;

                var report = new SalesReportDto
                {
                    Period = period,
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd"),
                    TotalRevenue = totalRevenue,
                    TotalTransactions = totalTransactions,
                    AverageTransaction = avgTransaction,
                    MonthlyGrowth = Math.Round((double)monthlyGrowth, 2),
                    SalesByPaymentMethod = sales
                        .Where(s => s.Status == "completed")
                        .GroupBy(s => s.PaymentMethod)
                        .ToDictionary(g => g.Key, g => g.Sum(s => s.Total)),
                    SalesByStatus = sales
                        .GroupBy(s => s.Status)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("sales/{id}/receipt")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sale == null)
                {
                    return NotFound(new { error = "Sale not found" });
                }

                // Generate PDF receipt
                var pdfContent = GeneratePdfReceipt(sale);
                var fileName = $"receipt_{sale.ReceiptNumber}.pdf";

                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(pdfContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt PDF");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private byte[] GeneratePdfReceipt(Sale sale)
        {
            // Simple PDF generation using HTML to PDF approach
            // For production, consider using a proper PDF library like iTextSharp or PdfSharp
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Receipt {sale.ReceiptNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: #1c6db8; }}
        .info {{ margin-bottom: 20px; }}
        .items {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
        .items th, .items td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        .items th {{ background-color: #f2f2f2; }}
        .total {{ text-align: right; font-weight: bold; margin-top: 20px; }}
        .footer {{ margin-top: 30px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>UMI HEALTH PHARMACY</h1>
        <p>Receipt #: {sale.ReceiptNumber}</p>
        <p>Date: {sale.CreatedAt:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <div class='info'>
        <p><strong>Customer:</strong> {sale.Customer?.Name ?? "Walk-in"}</p>
        <p><strong>Payment Method:</strong> {sale.PaymentMethod}</p>
        <p><strong>Status:</strong> {sale.Status}</p>
    </div>
    
    <table class='items'>
        <thead>
            <tr>
                <th>Product</th>
                <th>Qty</th>
                <th>Unit Price</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>
                    {string.Join("", sale.SaleItems.Select(item => $@"
            <tr>
                <td>{item.Product.Name}</td>
                <td>{item.Quantity}</td>
                <td>ZMW {item.UnitPrice:F2}</td>
                <td>ZMW {item.TotalPrice:F2}</td>
            </tr>"))}
        </tbody>
    </table>
    
    <div class='total'>
        <p>Subtotal: ZMW {sale.Subtotal:F2}</p>
        <p>Tax: ZMW {sale.Tax:F2}</p>
        <p><strong>Total: ZMW {sale.Total:F2}</strong></p>
        {(sale.CashReceived > 0 ? $"<p>Cash Received: ZMW {sale.CashReceived:F2}</p><p>Change: ZMW {sale.Change:F2}</p>" : "")}
    </div>
    
    <div class='footer'>
        <p>Thank you for your business!</p>
        <p>UMI Health Pharmacy - Zambia</p>
    </div>
</body>
</html>";

            // Convert HTML to bytes (simplified approach)
            // In production, use a proper HTML to PDF converter
            var bytes = Encoding.UTF8.GetBytes(htmlContent);

            // For now, return as HTML file that can be printed as PDF
            // TODO: Implement proper PDF generation
            return bytes;
        }
    }

    // Request/Response Models
    public class AddStaffRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class StockUpdateRequest
    {
        public List<StockUpdateItem> Items { get; set; }
    }

    public class StockUpdateItem
    {
        public int ProductId { get; set; }
        public int OldStock { get; set; }
        public int NewStock { get; set; }
        public string Reason { get; set; }
    }

    // Additional model classes for dashboard functionality
    public class LowStockItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ReportSummary
    {
        public string Period { get; set; }
        public string TotalRevenue { get; set; }
        public int TotalSales { get; set; }
        public List<TopProduct> TopProducts { get; set; }
    }

    public class TopProduct
    {
        public string Name { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RegistrationNumber { get; set; }
        public string Specialization { get; set; }
    }
}
