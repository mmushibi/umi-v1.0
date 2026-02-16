using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionHistoryController : ControllerBase
    {
        private readonly ISubscriptionHistoryService _subscriptionHistoryService;
        private readonly ILogger<SubscriptionHistoryController> _logger;

        public SubscriptionHistoryController(
            ISubscriptionHistoryService subscriptionHistoryService,
            ILogger<SubscriptionHistoryController> logger)
        {
            _subscriptionHistoryService = subscriptionHistoryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<SubscriptionHistoryDto>>> GetSubscriptionHistory(
            [FromQuery] string searchQuery = "",
            [FromQuery] string action = "",
            [FromQuery] string plan = "",
            [FromQuery] string dateRange = "")
        {
            try
            {
                var history = await _subscriptionHistoryService.GetSubscriptionHistoryAsync(searchQuery, action, plan, dateRange);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription history");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubscriptionHistoryDto>> GetSubscriptionHistory(int id)
        {
            try
            {
                var history = await _subscriptionHistoryService.GetSubscriptionHistoryByIdAsync(id);
                if (history == null)
                {
                    return NotFound(new { error = "Subscription history record not found" });
                }
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription history with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<SubscriptionHistory>> CreateSubscriptionHistory([FromBody] CreateSubscriptionHistoryRequest request)
        {
            try
            {
                var history = await _subscriptionHistoryService.CreateSubscriptionHistoryAsync(request);
                return CreatedAtAction(nameof(GetSubscriptionHistory), new { id = history.Id }, history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription history");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<SubscriptionHistory>> UpdateSubscriptionHistory(int id, [FromBody] UpdateSubscriptionHistoryRequest request)
        {
            try
            {
                var history = await _subscriptionHistoryService.UpdateSubscriptionHistoryAsync(id, request);
                if (history == null)
                {
                    return NotFound(new { error = "Subscription history record not found" });
                }
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription history with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteSubscriptionHistory(int id)
        {
            try
            {
                var result = await _subscriptionHistoryService.DeleteSubscriptionHistoryAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Subscription history record not found" });
                }
                return Ok(new { success = true, message = "Subscription history record deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription history with ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportSubscriptionHistoryToCsv(
            [FromQuery] string searchQuery = "",
            [FromQuery] string action = "",
            [FromQuery] string plan = "",
            [FromQuery] string dateRange = "")
        {
            try
            {
                var csvContent = await _subscriptionHistoryService.ExportSubscriptionHistoryToCsvAsync(searchQuery, action, plan, dateRange);
                var fileName = $"subscription-history-{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(csvContent, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting subscription history to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("tenant/{subscriptionId}/export-csv")]
        public async Task<IActionResult> ExportTenantDetailsToCsv(int subscriptionId)
        {
            try
            {
                var csvContent = await _subscriptionHistoryService.ExportTenantDetailsToCsvAsync(subscriptionId);
                var subscription = await _subscriptionHistoryService.GetSubscriptionHistoryByIdAsync(subscriptionId);
                var tenantName = subscription?.Tenant?.Replace(" ", "-").ToLower() ?? "unknown";
                var fileName = $"tenant-details-{tenantName}-{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(csvContent, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting tenant details to CSV");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("tenant/{tenantName}/history")]
        public async Task<ActionResult<List<SubscriptionHistoryDto>>> GetTenantHistory(string tenantName)
        {
            try
            {
                var history = await _subscriptionHistoryService.GetTenantHistoryAsync(tenantName);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant history for {TenantName}", tenantName);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetSubscriptionStats()
        {
            try
            {
                var history = await _subscriptionHistoryService.GetSubscriptionHistoryAsync();
                
                var stats = new
                {
                    totalRecords = history.Count,
                    renewals = history.Count(h => h.Action == "renewed"),
                    cancellations = history.Count(h => h.Action == "cancelled"),
                    totalRevenue = history
                        .Where(h => h.Action != "cancelled")
                        .Sum(h => decimal.Parse(h.Amount))
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}


