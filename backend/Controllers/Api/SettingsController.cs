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
using UmiHealthPOS.Services;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;
        private readonly IRealSettingsService _settingsService;

        public SettingsController(
            ApplicationDbContext context,
            ILogger<SettingsController> logger,
            IRealSettingsService settingsService)
        {
            _context = context;
            _logger = logger;
            _settingsService = settingsService;
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

        // Frontend Integration Endpoints
        [HttpGet("frontend")]
        public async Task<ActionResult<Dictionary<string, object>>> GetFrontendSettings()
        {
            try
            {
                var settings = await _settingsService.GetAllSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving frontend settings");
                return StatusCode(500, new { message = "Error retrieving settings" });
            }
        }

        [HttpPost("frontend/{category}")]
        public async Task<ActionResult> SaveFrontendSettings(string category, [FromBody] Dictionary<string, object> settings)
        {
            try
            {
                await _settingsService.SaveSettingsAsync(category, settings);
                return Ok(new { message = $"{category} settings saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {Category} frontend settings", category);
                return StatusCode(500, new { message = $"Error saving {category} settings" });
            }
        }

        [HttpPost("frontend/email/test")]
        public async Task<ActionResult> SendTestEmail()
        {
            try
            {
                var result = await _settingsService.SendTestEmailAsync();
                if (result)
                {
                    return Ok(new { message = "Test email sent successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to send test email. Please check your email configuration." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(500, new { message = "Error sending test email" });
            }
        }

        [HttpPost("frontend/backup/create")]
        public async Task<ActionResult> CreateBackup()
        {
            try
            {
                var backupId = await _settingsService.CreateBackupAsync();
                return Ok(new { message = "Backup created successfully", backupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return StatusCode(500, new { message = "Error creating backup" });
            }
        }

        [HttpGet("frontend/backup/download/latest")]
        public async Task<ActionResult> DownloadLatestBackup()
        {
            try
            {
                var backupData = await _settingsService.DownloadLatestBackupAsync();
                return File(backupData, "application/zip", $"umihealth-backup-{DateTime.UtcNow:yyyy-MM-dd}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading backup");
                return StatusCode(500, new { message = "Error downloading backup" });
            }
        }

        [HttpPost("frontend/system/updates/check")]
        public async Task<ActionResult> CheckForUpdates()
        {
            try
            {
                var updateInfo = await _settingsService.CheckForUpdatesAsync();
                return Ok(updateInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return StatusCode(500, new { message = "Error checking for updates" });
            }
        }

        [HttpGet("frontend/system/logs/download")]
        public async Task<ActionResult> DownloadSystemLogs()
        {
            try
            {
                var logData = await _settingsService.DownloadSystemLogsAsync();
                return File(logData, "application/zip", $"umihealth-logs-{DateTime.UtcNow:yyyy-MM-dd}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading system logs");
                return StatusCode(500, new { message = "Error downloading system logs" });
            }
        }

        // Advanced Settings Management Endpoints

        [HttpGet("categories")]
        public async Task<ActionResult<List<SettingsCategoryDto>>> GetSettingsCategories()
        {
            try
            {
                var categories = await _context.AppSettings
                    .GroupBy(s => s.Category)
                    .Select(g => new SettingsCategoryDto
                    {
                        Name = g.Key,
                        DisplayName = GetCategoryDisplayName(g.Key),
                        Description = GetCategoryDescription(g.Key),
                        Icon = GetCategoryIcon(g.Key),
                        SettingCount = g.Count(),
                        Order = GetCategoryOrder(g.Key)
                    })
                    .OrderBy(c => c.Order)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings categories");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportSettings([FromQuery] string? category = null)
        {
            try
            {
                var query = _context.AppSettings.AsQueryable();
                
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(s => s.Category == category);
                }

                var settings = await query
                    .OrderBy(s => s.Category)
                    .ThenBy(s => s.Key)
                    .ToListAsync();

                var exportData = new
                {
                    ExportDate = DateTime.UtcNow,
                    Category = category ?? "All",
                    TotalSettings = settings.Count,
                    Settings = settings.Select(s => new
                    {
                        s.Category,
                        s.Key,
                        s.Value,
                        s.Description,
                        s.DataType,
                        s.Environment,
                        s.IsEncrypted,
                        s.CreatedAt,
                        s.UpdatedAt
                    })
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                var fileName = $"umihealth-settings-{(category ?? "all")}-{DateTime.UtcNow:yyyy-MM-dd}.json";

                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("import")]
        public async Task<ActionResult<ImportResultDto>> ImportSettings([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var importData = JsonSerializer.Deserialize<SettingsImportData>(json);
                if (importData?.Settings == null)
                {
                    return BadRequest(new { error = "Invalid import file format" });
                }

                var result = new ImportResultDto
                {
                    TotalRecords = importData.Settings.Count,
                    SuccessCount = 0,
                    FailedCount = 0,
                    Errors = new List<string>()
                };

                foreach (var settingData in importData.Settings)
                {
                    try
                    {
                        var existingSetting = await _context.AppSettings
                            .FirstOrDefaultAsync(s => s.Category == settingData.Category && s.Key == settingData.Key);

                        if (existingSetting != null)
                        {
                            existingSetting.Value = settingData.Value;
                            existingSetting.Description = settingData.Description;
                            existingSetting.DataType = settingData.DataType;
                            existingSetting.UpdatedAt = DateTime.UtcNow;
                            existingSetting.UpdatedBy = GetCurrentUserId();
                        }
                        else
                        {
                            var newSetting = new AppSetting
                            {
                                Category = settingData.Category,
                                Key = settingData.Key,
                                Value = settingData.Value,
                                Description = settingData.Description,
                                DataType = settingData.DataType,
                                Environment = settingData.Environment ?? "Production",
                                IsEncrypted = settingData.IsEncrypted,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = GetCurrentUserId()
                            };

                            await _context.AppSettings.AddAsync(newSetting);
                        }

                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to import setting {settingData.Category}.{settingData.Key}: {ex.Message}");
                        _logger.LogError(ex, "Error importing setting {Category}.{Key}", settingData.Category, settingData.Key);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ValidationResultDto>> ValidateSettings([FromBody] List<SettingValidationRequest> requests)
        {
            try
            {
                var result = new ValidationResultDto
                {
                    IsValid = true,
                    Warnings = new List<string>(),
                    Errors = new List<string>()
                };

                foreach (var request in requests)
                {
                    var validation = await ValidateSettingAsync(request);
                    
                    if (!validation.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(validation.Errors);
                    }
                    
                    result.Warnings.AddRange(validation.Warnings);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("reset")]
        public async Task<ActionResult> ResetSettingsToDefault([FromBody] ResetSettingsRequest request)
        {
            try
            {
                var query = _context.AppSettings.AsQueryable();
                
                if (!string.IsNullOrEmpty(request.Category))
                {
                    query = query.Where(s => s.Category == request.Category);
                }

                if (request.Keys?.Any() == true)
                {
                    query = query.Where(s => request.Keys.Contains(s.Key));
                }

                var settingsToReset = await query.ToListAsync();
                
                foreach (var setting in settingsToReset)
                {
                    var defaultValue = GetDefaultValue(setting.Category, setting.Key);
                    if (defaultValue != null)
                    {
                        setting.Value = defaultValue;
                        setting.UpdatedAt = DateTime.UtcNow;
                        setting.UpdatedBy = GetCurrentUserId();
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Settings reset to default values successfully", 
                    resetCount = settingsToReset.Count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("audit")]
        public async Task<ActionResult<PagedResult<SettingsAuditLogDto>>> GetSettingsAuditLogs([FromQuery] SettingsAuditFilterDto filter)
        {
            try
            {
                var query = _context.SettingsAuditLogs.AsQueryable();

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(l => l.Category == filter.Category);
                }

                if (!string.IsNullOrEmpty(filter.Key))
                {
                    query = query.Where(l => l.Key.Contains(filter.Key));
                }

                if (!string.IsNullOrEmpty(filter.Action))
                {
                    query = query.Where(l => l.Action == filter.Action);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp <= filter.EndDate.Value);
                }

                var totalCount = await query.CountAsync();
                var auditLogs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(l => new SettingsAuditLogDto
                    {
                        Id = l.Id,
                        Category = l.Category,
                        Key = l.Key,
                        Action = l.Action,
                        OldValue = l.OldValue,
                        NewValue = l.NewValue,
                        UserId = l.UserId,
                        Description = l.Description,
                        IpAddress = l.IpAddress,
                        UserAgent = l.UserAgent,
                        Timestamp = l.Timestamp
                    })
                    .ToListAsync();

                return Ok(new PagedResult<SettingsAuditLogDto>
                {
                    Data = auditLogs,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings audit logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Helper methods
        private string GetCurrentUserId()
        {
            return User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value ?? "unknown";
        }

        private string GetCategoryDisplayName(string category)
        {
            return category switch
            {
                "system" => "System Settings",
                "email" => "Email Configuration",
                "security" => "Security Settings",
                "backup" => "Backup Settings",
                "notification" => "Notification Settings",
                "integration" => "Integration Settings",
                "ui" => "User Interface",
                _ => category
            };
        }

        private string GetCategoryDescription(string category)
        {
            return category switch
            {
                "system" => "Core system configuration and settings",
                "email" => "SMTP server and email delivery settings",
                "security" => "Authentication, authorization, and security policies",
                "backup" => "Automated backup and recovery settings",
                "notification" => "Real-time notification and alert preferences",
                "integration" => "Third-party service integrations and APIs",
                "ui" => "User interface customization and preferences",
                _ => "Configuration settings"
            };
        }

        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "system" => "fas fa-cog",
                "email" => "fas fa-envelope",
                "security" => "fas fa-shield-alt",
                "backup" => "fas fa-database",
                "notification" => "fas fa-bell",
                "integration" => "fas fa-plug",
                "ui" => "fas fa-palette",
                _ => "fas fa-cog"
            };
        }

        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "system" => 1,
                "security" => 2,
                "email" => 3,
                "notification" => 4,
                "backup" => 5,
                "integration" => 6,
                "ui" => 7,
                _ => 999
            };
        }

        private async Task<SettingValidationResult> ValidateSettingAsync(SettingValidationRequest request)
        {
            var result = new SettingValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            // Validate based on data type
            switch (request.DataType?.ToLower())
            {
                case "email":
                    if (!IsValidEmail(request.Value))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid email format: {request.Value}");
                    }
                    break;
                case "url":
                    if (!IsValidUrl(request.Value))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid URL format: {request.Value}");
                    }
                    break;
                case "number":
                    if (!double.TryParse(request.Value, out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid number format: {request.Value}");
                    }
                    break;
                case "boolean":
                    if (!bool.TryParse(request.Value, out _) && 
                        !request.Value.Equals("true", StringComparison.OrdinalIgnoreCase) && 
                        !request.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid boolean format: {request.Value}");
                    }
                    break;
            }

            // Validate specific settings
            if (request.Category == "email" && request.Key == "SmtpPort")
            {
                if (int.TryParse(request.Value, out var port) && (port < 1 || port > 65535))
                {
                    result.IsValid = false;
                    result.Errors.Add("SMTP port must be between 1 and 65535");
                }
            }

            return result;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        private string? GetDefaultValue(string category, string key)
        {
            return (category, key) switch
            {
                ("system", "MaintenanceMode") => "false",
                ("system", "TimeZone") => "Africa/Lusaka",
                ("email", "SmtpPort") => "587",
                ("email", "EnableSsl") => "true",
                ("security", "SessionTimeoutMinutes") => "30",
                ("security", "MaxLoginAttempts") => "5",
                ("notification", "EnableRealTime") => "true",
                ("backup", "AutoBackupEnabled") => "true",
                ("backup", "BackupRetentionDays") => "30",
                _ => null
            };
        }
    }
}


