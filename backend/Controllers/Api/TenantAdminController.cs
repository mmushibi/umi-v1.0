using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models.Dashboard;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantAdminController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<TenantAdminController> _logger;

        public TenantAdminController(
            IDashboardService dashboardService,
            ILogger<TenantAdminController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
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

        [HttpGet("inventory/low-stock")]
        public async Task<ActionResult<List<LowStockItem>>> GetLowStockItems()
        {
            try
            {
                // Return empty list - no mock data
                // When database is implemented, this will query real inventory data
                var lowStockItems = new List<LowStockItem>();
                
                return Ok(lowStockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock items");
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
