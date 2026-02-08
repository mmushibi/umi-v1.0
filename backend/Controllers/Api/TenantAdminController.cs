using Microsoft.AspNetCore.Mvc;
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

        public TenantAdminController(
            IDashboardService dashboardService,
            ILogger<TenantAdminController> logger,
            IInventoryService inventoryService)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _inventoryService = inventoryService;
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
}
