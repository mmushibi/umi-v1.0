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
using Microsoft.AspNetCore.Http;
using System.IO;
using UmiHealthPOS.Services;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _context;

        public InventoryController(
            ILogger<InventoryController> logger,
            IInventoryService inventoryService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _inventoryService = inventoryService;
            _context = context;
        }

        [HttpGet("items")]
        public async Task<ActionResult<List<InventoryItem>>> GetInventoryItems()
        {
            try
            {
                var items = await _inventoryService.GetInventoryItemsAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory items");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("items")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<InventoryItem>> CreateInventoryItem([FromBody] CreateInventoryItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var item = await _inventoryService.CreateInventoryItemAsync(request);
                return CreatedAtAction(nameof(GetInventoryItems), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory item");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("items/{id}")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<InventoryItem>> UpdateInventoryItem(int id, [FromBody] UpdateInventoryItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid request data", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var item = await _inventoryService.UpdateInventoryItemAsync(id, request);
                if (item == null)
                {
                    return NotFound(new { message = "Inventory item not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory item {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("items/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteInventoryItem(int id)
        {
            try
            {
                var result = await _inventoryService.DeleteInventoryItemAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Inventory item not found" });
                }

                return Ok(new { message = "Inventory item deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory item {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("import-csv")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<CsvImportResult>> ImportInventoryFromCsv([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Only CSV files are allowed" });
                }

                var result = await _inventoryService.ImportInventoryFromCsvAsync(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing inventory from CSV");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportInventoryToCsv()
        {
            try
            {
                var csvBytes = await _inventoryService.ExportInventoryToCsvAsync();
                var fileName = $"inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory to CSV");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("download-template")]
        public IActionResult DownloadCsvTemplate()
        {
            try
            {
                // Generate a simple CSV template
                var template = "Inventory Item Name,Generic Name,Brand Name,Manufacture Date,Batch Number,License Number,Zambia REG Number,Packing Type,Quantity,Unit Price,Selling Price,Reorder Level\n" +
                              "Sample Item,Sample Generic,Sample Brand,2024-01-01,B001,LICENSE001,ZAMBIA001,Box,100,50.00,75.00,10\n";

                var fileName = "inventory_template.csv";

                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
                return File(Encoding.UTF8.GetBytes(template), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading CSV template");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}


