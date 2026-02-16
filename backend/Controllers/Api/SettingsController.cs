using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.DTOs;
using System.Security.Claims;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ApplicationDbContext context,
            ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<AppSettingDto>>> GetSettings([FromQuery] AppSettingFilterDto filter)
        {
            try
            {
                var query = _context.AppSettings.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(s => s.Category == filter.Category);
                }

                if (!string.IsNullOrEmpty(filter.Environment))
                {
                    query = query.Where(s => s.Environment == filter.Environment);
                }

                if (filter.IsEncrypted.HasValue)
                {
                    query = query.Where(s => s.IsEncrypted == filter.IsEncrypted.Value);
                }

                if (filter.IsReadOnly.HasValue)
                {
                    query = query.Where(s => s.IsReadOnly == filter.IsReadOnly.Value);
                }

                if (!string.IsNullOrEmpty(filter.Search))
                {
                    query = query.Where(s =>
                        s.Key.Contains(filter.Search) ||
                        s.Description.Contains(filter.Search));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var settings = await query
                    .OrderBy(s => s.Category)
                    .ThenBy(s => s.Key)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(s => new AppSettingDto
                    {
                        Id = s.Id,
                        Key = s.Key,
                        Value = s.Value,
                        Description = s.Description,
                        Category = s.Category,
                        DataType = s.DataType,
                        IsEncrypted = s.IsEncrypted,
                        IsReadOnly = s.IsReadOnly,
                        Environment = s.Environment,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        UpdatedBy = s.UpdatedBy,
                        DisplayValue = s.IsEncrypted ? "***" : s.Value
                    })
                    .ToListAsync();

                return Ok(new PagedResult<AppSettingDto>
                {
                    Data = settings,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<AppSettingStatsDto>> GetSettingsStats()
        {
            try
            {
                var query = _context.AppSettings.AsQueryable();

                var totalSettings = await query.CountAsync();
                var encryptedSettings = await query.CountAsync(s => s.IsEncrypted);
                var readOnlySettings = await query.CountAsync(s => s.IsReadOnly);

                var categoryCounts = await query
                    .GroupBy(s => s.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);

                var environmentCounts = await query
                    .GroupBy(s => s.Environment)
                    .Select(g => new { Environment = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Environment, x => x.Count);

                return Ok(new AppSettingStatsDto
                {
                    TotalSettings = totalSettings,
                    EncryptedSettings = encryptedSettings,
                    ReadOnlySettings = readOnlySettings,
                    CategoryCounts = categoryCounts,
                    EnvironmentCounts = environmentCounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppSettingDto>> GetSetting(int id)
        {
            try
            {
                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    return NotFound(new { error = "Setting not found" });
                }

                return Ok(new AppSettingDto
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    Category = setting.Category,
                    DataType = setting.DataType,
                    IsEncrypted = setting.IsEncrypted,
                    IsReadOnly = setting.IsReadOnly,
                    Environment = setting.Environment,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt,
                    UpdatedBy = setting.UpdatedBy,
                    DisplayValue = setting.IsEncrypted ? "***" : setting.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("key/{key}")]
        public async Task<ActionResult<AppSettingDto>> GetSettingByKey(string key)
        {
            try
            {
                var setting = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting == null)
                {
                    return NotFound(new { error = "Setting not found" });
                }

                return Ok(new AppSettingDto
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    Category = setting.Category,
                    DataType = setting.DataType,
                    IsEncrypted = setting.IsEncrypted,
                    IsReadOnly = setting.IsReadOnly,
                    Environment = setting.Environment,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt,
                    UpdatedBy = setting.UpdatedBy,
                    DisplayValue = setting.IsEncrypted ? "***" : setting.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting by key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AppSettingDto>> CreateSetting([FromBody] CreateAppSettingRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if setting key already exists
                var existingSetting = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == request.Key);

                if (existingSetting != null)
                {
                    return BadRequest(new { error = "Setting key already exists" });
                }

                var setting = new AppSetting
                {
                    Key = request.Key,
                    Value = request.Value,
                    Description = request.Description,
                    Category = request.Category,
                    DataType = request.DataType,
                    IsEncrypted = request.IsEncrypted,
                    IsReadOnly = request.IsReadOnly,
                    Environment = request.Environment,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = currentUserId
                };

                await _context.AppSettings.AddAsync(setting);
                await _context.SaveChangesAsync();

                var settingDto = new AppSettingDto
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    Category = setting.Category,
                    DataType = setting.DataType,
                    IsEncrypted = setting.IsEncrypted,
                    IsReadOnly = setting.IsReadOnly,
                    Environment = setting.Environment,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt,
                    UpdatedBy = setting.UpdatedBy,
                    DisplayValue = setting.IsEncrypted ? "***" : setting.Value
                };

                return CreatedAtAction(nameof(GetSetting), new { id = setting.Id }, settingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating setting");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AppSettingDto>> UpdateSetting(int id, [FromBody] UpdateAppSettingRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    return NotFound(new { error = "Setting not found" });
                }

                if (setting.IsReadOnly)
                {
                    return BadRequest(new { error = "Cannot modify read-only setting" });
                }

                // Check if setting key already exists (excluding this setting)
                var existingSetting = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == request.Key && s.Id != id);

                if (existingSetting != null)
                {
                    return BadRequest(new { error = "Setting key already exists" });
                }

                setting.Value = request.Value;
                setting.Description = request.Description;
                setting.DataType = request.DataType;
                setting.IsEncrypted = request.IsEncrypted;
                setting.IsReadOnly = request.IsReadOnly;
                setting.Environment = request.Environment;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = currentUserId;

                await _context.SaveChangesAsync();

                var settingDto = new AppSettingDto
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    Category = setting.Category,
                    DataType = setting.DataType,
                    IsEncrypted = setting.IsEncrypted,
                    IsReadOnly = setting.IsReadOnly,
                    Environment = setting.Environment,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt,
                    UpdatedBy = setting.UpdatedBy,
                    DisplayValue = setting.IsEncrypted ? "***" : setting.Value
                };

                return Ok(settingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}/toggle-readonly")]
        public async Task<ActionResult> ToggleReadOnlyStatus(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    return NotFound(new { error = "Setting not found" });
                }

                setting.IsReadOnly = !setting.IsReadOnly;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = currentUserId;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Setting read-only status toggled to {setting.IsReadOnly}",
                    isReadOnly = setting.IsReadOnly
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling setting read-only status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSetting(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    return NotFound(new { error = "Setting not found" });
                }

                if (setting.IsReadOnly)
                {
                    return BadRequest(new { error = "Cannot delete read-only setting" });
                }

                _context.AppSettings.Remove(setting);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Setting deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting setting");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            try
            {
                var categories = await _context.AppSettings
                    .Select(s => s.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportSettings([FromQuery] AppSettingFilterDto filter)
        {
            try
            {
                // Set a large page size for export
                filter.PageSize = 10000;
                filter.Page = 1;

                var result = await GetSettings(filter);
                if (result.Result is OkObjectResult okResult && okResult.Value is PagedResult<AppSettingDto> pagedResult)
                {
                    var csv = GenerateSettingsCsv(pagedResult.Data);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                    return File(bytes, "text/csv", $"settings_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                }

                return StatusCode(500, new { error = "Failed to export settings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("import")]
        public async Task<ActionResult> ImportSettings([FromForm] IFormFile file)
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

                var settings = new List<CreateAppSettingRequest>();
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    var header = reader.ReadLine();
                    if (header == null)
                    {
                        return BadRequest(new { error = "Empty file" });
                    }

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');
                        if (values.Length >= 4)
                        {
                            settings.Add(new CreateAppSettingRequest
                            {
                                Key = values[0]?.Trim() ?? string.Empty,
                                Value = values[1]?.Trim() ?? string.Empty,
                                Description = values[2]?.Trim() ?? string.Empty,
                                Category = values[3]?.Trim() ?? string.Empty,
                                DataType = values.Length > 4 ? values[4]?.Trim() : "String",
                                IsEncrypted = values.Length > 5 && bool.TryParse(values[5], out var encrypted) && encrypted,
                                IsReadOnly = values.Length > 6 && bool.TryParse(values[6], out var readOnly) && readOnly,
                                Environment = values.Length > 7 ? values[7]?.Trim() : "All"
                            });
                        }
                    }
                }

                var importedCount = 0;
                var errors = new List<string>();

                foreach (var setting in settings)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(setting.Key))
                        {
                            errors.Add($"Skipping invalid setting with empty key");
                            continue;
                        }

                        var existingSetting = await _context.AppSettings
                            .FirstOrDefaultAsync(s => s.Key == setting.Key);

                        if (existingSetting != null)
                        {
                            errors.Add($"Setting key {setting.Key} already exists");
                            continue;
                        }

                        var newSetting = new AppSetting
                        {
                            Key = setting.Key,
                            Value = setting.Value,
                            Description = setting.Description,
                            Category = setting.Category,
                            DataType = setting.DataType,
                            IsEncrypted = setting.IsEncrypted,
                            IsReadOnly = setting.IsReadOnly,
                            Environment = setting.Environment,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        };

                        await _context.AppSettings.AddAsync(newSetting);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error importing setting {setting.Key}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Imported {importedCount} settings successfully",
                    importedCount = importedCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private string GenerateSettingsCsv(List<AppSettingDto> settings)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Key,Value,Description,Category,Data Type,Is Encrypted,Is Read Only,Environment,Created At,Updated At");

            foreach (var setting in settings)
            {
                csv.AppendLine($"\"{setting.Key}\"," +
                    $"\"{setting.Value?.Replace("\"", "\"\"")}\"," +
                    $"\"{setting.Description?.Replace("\"", "\"\"")}\"," +
                    $"{setting.Category}," +
                    $"{setting.DataType}," +
                    $"{setting.IsEncrypted}," +
                    $"{setting.IsReadOnly}," +
                    $"{setting.Environment}," +
                    $"{setting.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                    $"{setting.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return csv.ToString();
        }
    }
}


