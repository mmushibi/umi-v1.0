using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models.Dashboard;
using UmiHealthPOS.Services;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantAdminController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<TenantAdminController> _logger;
        private readonly IInventoryService _inventoryService;
        private readonly IPrescriptionService _prescriptionService;

        public TenantAdminController(
            IDashboardService dashboardService,
            ILogger<TenantAdminController> logger,
            IInventoryService inventoryService,
            IPrescriptionService prescriptionService)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _inventoryService = inventoryService;
            _prescriptionService = prescriptionService;
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
                    return Ok(new { 
                        success = true, 
                        message = result.Message, 
                        saleId = result.SaleId 
                    });
                }
                else
                {
                    return BadRequest(new { 
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
                return Ok(new { 
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
                
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
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
                
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
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
