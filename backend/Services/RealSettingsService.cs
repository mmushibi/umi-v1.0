using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace UmiHealthPOS.Services
{
    public interface IRealSettingsService
    {
        Task<Dictionary<string, object>> GetAllSettingsAsync();
        Task SaveSettingsAsync(string category, Dictionary<string, object> settings);
        Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);
        Task SetSettingAsync(string key, object value, string category = "general", string description = "");
        Task<bool> SendTestEmailAsync();
        Task<string> CreateBackupAsync();
        Task<byte[]> DownloadLatestBackupAsync();
        Task<UpdateInfo> CheckForUpdatesAsync();
        Task<byte[]> DownloadSystemLogsAsync();
    }

    public class RealSettingsService : IRealSettingsService
    {
        private readonly ILogger<RealSettingsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IBackupService _backupService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthService _authService;

        public RealSettingsService(
            ILogger<RealSettingsService> logger,
            IConfiguration configuration,
            ApplicationDbContext context,
            IEmailService emailService,
            IBackupService backupService,
            IHttpContextAccessor httpContextAccessor,
            IAuthService authService)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
            _backupService = backupService;
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
        }

        public async Task<Dictionary<string, object>> GetAllSettingsAsync()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["general"] = await GetGeneralSettingsAsync(),
                    ["security"] = await GetSecuritySettingsAsync(),
                    ["backup"] = await GetBackupSettingsAsync(),
                    ["email"] = await GetEmailSettingsAsync(),
                    ["integrations"] = await GetIntegrationSettingsAsync(),
                    ["system"] = await GetSystemSettingsAsync()
                };

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all settings");
                throw;
            }
        }

        public async Task SaveSettingsAsync(string category, Dictionary<string, object> settings)
        {
            try
            {
                _logger.LogInformation("Saving {Category} settings", category);

                foreach (var setting in settings)
                {
                    await SetSettingAsync(setting.Key, setting.Value, category);
                }

                // Log the settings change
                await LogSettingsChangeAsync(category, settings);

                _logger.LogInformation("Successfully saved {Category} settings", category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {Category} settings", category);
                throw;
            }
        }

        public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
        {
            try
            {
                var setting = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting == null || string.IsNullOrEmpty(setting.Value))
                {
                    return defaultValue;
                }

                // Convert the string value to the appropriate type
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(setting.Value);
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(setting.Value);
                }
                else if (typeof(T) == typeof(decimal))
                {
                    return (T)(object)decimal.Parse(setting.Value);
                }
                else
                {
                    return (T)(object)setting.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting setting {Key}", key);
                return defaultValue;
            }
        }

        public async Task SetSettingAsync(string key, object value, string category = "general", string description = "")
        {
            try
            {
                var existingSetting = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (existingSetting != null)
                {
                    if (existingSetting.IsReadOnly)
                    {
                        _logger.LogWarning("Attempted to modify read-only setting: {Key}", key);
                        return;
                    }

                    existingSetting.Value = value.ToString();
                    existingSetting.UpdatedAt = DateTime.UtcNow;
                    existingSetting.UpdatedBy = GetCurrentUserId();
                }
                else
                {
                    var newSetting = new AppSetting
                    {
                        Key = key,
                        Value = value.ToString(),
                        Description = description,
                        Category = category,
                        DataType = GetDataType(value),
                        IsEncrypted = key.Contains("password") || key.Contains("secret") || key.Contains("key"),
                        IsReadOnly = false,
                        Environment = "Production",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = GetCurrentUserId()
                    };

                    _context.AppSettings.Add(newSetting);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting {Key} = {Value}", key, value);
                throw;
            }
        }

        public async Task<bool> SendTestEmailAsync()
        {
            try
            {
                return await _emailService.SendTestEmailAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return false;
            }
        }

        public async Task<string> CreateBackupAsync()
        {
            try
            {
                return await _backupService.CreateBackupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                throw;
            }
        }

        public async Task<byte[]> DownloadLatestBackupAsync()
        {
            try
            {
                return await _backupService.GetLatestBackupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading latest backup");
                throw;
            }
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                // Check against configured update service endpoint
                var updateServiceUrl = _configuration["UpdateService:Url"];
                if (!string.IsNullOrEmpty(updateServiceUrl))
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    var currentVersion = await GetSettingAsync("systemVersion", "v2.1.0");
                    var response = await httpClient.GetAsync($"{updateServiceUrl}/api/updates/check?version={currentVersion}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var updateInfo = await response.Content.ReadFromJsonAsync<UpdateInfo>();
                        await SetSettingAsync("lastUpdateCheck", DateTime.UtcNow);
                        return updateInfo ?? new UpdateInfo
                        {
                            UpdateAvailable = false,
                            CurrentVersion = currentVersion ?? "v2.1.0",
                            LatestVersion = currentVersion ?? "v2.1.0",
                            LastUpdateCheck = DateTime.UtcNow,
                            UpdateNotes = new List<string>(),
                            CriticalUpdate = false
                        };
                    }
                }

                // Fallback to simulated check if no update service configured
                await Task.Delay(100);
                var currentVersionFallback = await GetSettingAsync("systemVersion", "v2.1.0");
                var lastUpdateCheck = await GetSettingAsync("lastUpdateCheck", DateTime.MinValue);

                return new UpdateInfo
                {
                    UpdateAvailable = false,
                    CurrentVersion = currentVersionFallback ?? "v2.1.0",
                    LatestVersion = "v2.1.0",
                    LastUpdateCheck = lastUpdateCheck is DateTime date ? date : DateTime.UtcNow,
                    UpdateNotes = new List<string>(),
                    CriticalUpdate = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                throw;
            }
        }

        public async Task<byte[]> DownloadSystemLogsAsync()
        {
            try
            {
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                var logFiles = new List<string>();

                if (Directory.Exists(logDirectory))
                {
                    logFiles = Directory.GetFiles(logDirectory, "*.log")
                        .Where(f => File.GetLastWriteTime(f) > DateTime.UtcNow.AddDays(-7))
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .Take(10)
                        .ToList();
                }

                using var memoryStream = new MemoryStream();
                using var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create);
                
                // Add log files to archive
                foreach (var logFile in logFiles)
                {
                    var fileName = Path.GetFileName(logFile);
                    var entry = archive.CreateEntry($"logs/{fileName}");
                    
                    using var fileStream = File.OpenRead(logFile);
                    using var entryStream = entry.Open();
                    await fileStream.CopyToAsync(entryStream);
                }

                // Add system information
                var infoEntry = archive.CreateEntry("system_info.txt");
                using var infoWriter = new System.IO.StreamWriter(infoEntry.Open());
                await infoWriter.WriteAsync(await GenerateSystemInfoAsync());

                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading system logs");
                throw;
            }
        }

        private async Task<Dictionary<string, object>> GetGeneralSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "general")
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["systemName"] = GetSettingValue(settings, "systemName") ?? "UmiHealth POS",
                ["defaultCurrency"] = GetSettingValue(settings, "defaultCurrency") ?? "Zambian Kwacha (K)",
                ["timeZone"] = GetSettingValue(settings, "timeZone") ?? "CAT (UTC+2)",
                ["companyName"] = GetSettingValue(settings, "companyName") ?? "UmiHealth Solutions",
                ["supportEmail"] = GetSettingValue(settings, "supportEmail") ?? "support@umihealth.com",
                ["phoneNumber"] = GetSettingValue(settings, "phoneNumber") ?? "+260 123 456 789",
                ["emailNotifications"] = bool.Parse(GetSettingValue(settings, "emailNotifications") ?? "true"),
                ["maintenanceMode"] = bool.Parse(GetSettingValue(settings, "maintenanceMode") ?? "false"),
                ["debugMode"] = bool.Parse(GetSettingValue(settings, "debugMode") ?? "false")
            };
        }

        private async Task<Dictionary<string, object>> GetSecuritySettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "security")
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["sessionTimeout"] = int.Parse(GetSettingValue(settings, "sessionTimeout") ?? "30"),
                ["passwordPolicy"] = GetSettingValue(settings, "passwordPolicy") ?? "Strong (8+ chars, mixed case)",
                ["maxLoginAttempts"] = int.Parse(GetSettingValue(settings, "maxLoginAttempts") ?? "5"),
                ["lockoutDuration"] = int.Parse(GetSettingValue(settings, "lockoutDuration") ?? "15"),
                ["passwordExpiry"] = int.Parse(GetSettingValue(settings, "passwordExpiry") ?? "90"),
                ["apiRateLimit"] = int.Parse(GetSettingValue(settings, "apiRateLimit") ?? "100"),
                ["twoFactorAuth"] = bool.Parse(GetSettingValue(settings, "twoFactorAuth") ?? "true"),
                ["apiRateLimiting"] = bool.Parse(GetSettingValue(settings, "apiRateLimiting") ?? "true"),
                ["ipWhitelisting"] = bool.Parse(GetSettingValue(settings, "ipWhitelisting") ?? "false"),
                ["forceHttps"] = bool.Parse(GetSettingValue(settings, "forceHttps") ?? "true")
            };
        }

        private async Task<Dictionary<string, object>> GetBackupSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "backup")
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["backupFrequency"] = GetSettingValue(settings, "backupFrequency") ?? "Daily",
                ["backupRetention"] = int.Parse(GetSettingValue(settings, "backupRetention") ?? "30"),
                ["backupLocation"] = GetSettingValue(settings, "backupLocation") ?? Path.Combine(Directory.GetCurrentDirectory(), "backups"),
                ["compressionLevel"] = GetSettingValue(settings, "compressionLevel") ?? "Standard",
                ["encryption"] = GetSettingValue(settings, "encryption") ?? "AES-256",
                ["cloudStorage"] = GetSettingValue(settings, "cloudStorage") ?? "None",
                ["autoBackup"] = bool.Parse(GetSettingValue(settings, "autoBackup") ?? "true"),
                ["backupVerification"] = bool.Parse(GetSettingValue(settings, "backupVerification") ?? "true"),
                ["emailBackupReports"] = bool.Parse(GetSettingValue(settings, "emailBackupReports") ?? "false")
            };
        }

        private async Task<Dictionary<string, object>> GetEmailSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "email")
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["smtpServer"] = GetSettingValue(settings, "smtpServer") ?? "smtp.gmail.com",
                ["port"] = int.Parse(GetSettingValue(settings, "port") ?? "587"),
                ["username"] = GetSettingValue(settings, "username") ?? "",
                ["password"] = GetSettingValue(settings, "password") ?? "",
                ["fromEmail"] = GetSettingValue(settings, "fromEmail") ?? "noreply@umihealth.com",
                ["fromName"] = GetSettingValue(settings, "fromName") ?? "UmiHealth POS",
                ["useSslTls"] = bool.Parse(GetSettingValue(settings, "useSslTls") ?? "true"),
                ["enableEmailLogging"] = bool.Parse(GetSettingValue(settings, "enableEmailLogging") ?? "true")
            };
        }

        private async Task<Dictionary<string, object>> GetIntegrationSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "integrations")
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["paymentGateway"] = new Dictionary<string, object>
                {
                    ["provider"] = GetSettingValue(settings, "paymentProvider") ?? "None",
                    ["apiKey"] = GetSettingValue(settings, "paymentApiKey") ?? ""
                },
                ["smsService"] = new Dictionary<string, object>
                {
                    ["provider"] = GetSettingValue(settings, "smsProvider") ?? "None",
                    ["apiKey"] = GetSettingValue(settings, "smsApiKey") ?? ""
                },
                ["cloudStorage"] = new Dictionary<string, object>
                {
                    ["provider"] = GetSettingValue(settings, "storageProvider") ?? "None",
                    ["accessKey"] = GetSettingValue(settings, "storageAccessKey") ?? ""
                }
            };
        }

        private async Task<Dictionary<string, object>> GetSystemSettingsAsync()
        {
            return new Dictionary<string, object>
            {
                ["version"] = "v2.1.0",
                ["lastUpdated"] = "Jan 10, 2024",
                ["database"] = "PostgreSQL 14.2",
                ["environment"] = "Production",
                ["apiVersion"] = "v1.0.0",
                ["serverUptime"] = "99.9%"
            };
        }

        private string? GetSettingValue(List<AppSetting> settings, string key)
        {
            return settings.FirstOrDefault(s => s.Key == key)?.Value;
        }

        private string GetDataType(object value)
        {
            if (value is bool) return "Boolean";
            if (value is int) return "Integer";
            if (value is decimal || value is double || value is float) return "Decimal";
            if (value is DateTime) return "DateTime";
            return "String";
        }

        private async Task LogSettingsChangeAsync(string category, Dictionary<string, object> settings)
        {
            try
            {
                var auditLog = new SettingsAuditLog
                {
                    Category = category,
                    UserId = GetCurrentUserId(),
                    Action = "Update",
                    OldValue = await GetPreviousValuesAsync(category, settings),
                    NewValue = System.Text.Json.JsonSerializer.Serialize(settings),
                    Description = $"Settings updated for category: {category}",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    Timestamp = DateTime.UtcNow
                };

                _context.SettingsAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging settings change for category {Category}", category);
            }
        }

        private async Task<string> GenerateSystemInfoAsync()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("UmiHealth POS System Information");
            info.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            info.AppendLine();

            // System information
            info.AppendLine("SYSTEM INFORMATION:");
            info.AppendLine($"Version: v2.1.0");
            info.AppendLine($"Environment: Production");
            info.AppendLine($"Operating System: {Environment.OSVersion}");
            info.AppendLine($"Machine Name: {Environment.MachineName}");
            info.AppendLine();

            // Database information
            info.AppendLine("DATABASE INFORMATION:");
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                info.AppendLine($"Database: {connection.Database}");
                info.AppendLine($"Server: {connection.DataSource}");
                info.AppendLine($"State: {connection.State}");
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                info.AppendLine($"Database Error: {ex.Message}");
            }
            info.AppendLine();

            // Application settings count
            info.AppendLine("APPLICATION SETTINGS:");
            var settingsCount = await _context.AppSettings.CountAsync();
            info.AppendLine($"Total Settings: {settingsCount}");
            
            var categories = await _context.AppSettings
                .GroupBy(s => s.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var cat in categories)
            {
                info.AppendLine($"  {cat.Category}: {cat.Count} settings");
            }
            info.AppendLine();

            // Recent activity
            info.AppendLine("RECENT SETTINGS CHANGES:");
            var recentChanges = await _context.SettingsAuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();
            
            foreach (var change in recentChanges)
            {
                info.AppendLine($"  {change.Timestamp:yyyy-MM-dd HH:mm:ss} - {change.Category} by {change.UserId}");
            }

            return info.ToString();
        }

        private string GetCurrentUserId()
        {
            try
            {
                if (_authService.IsAuthenticated())
                {
                    return _authService.GetCurrentUserId();
                }
                return "system";
            }
            catch
            {
                return "system";
            }
        }

        private string GetClientIpAddress()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return "system";

                // Check for X-Forwarded-For header (for load balancers/proxies)
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(",").FirstOrDefault()?.Trim() ?? "unknown";
                }

                // Check for X-Real-IP header
                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }

                // Fall back to remote IP address
                return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private string GetUserAgent()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return "system";

                return context.Request.Headers["User-Agent"].ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private async Task<string> GetPreviousValuesAsync(string category, Dictionary<string, object> newSettings)
        {
            try
            {
                var previousSettings = new Dictionary<string, object>();
                
                foreach (var key in newSettings.Keys)
                {
                    var existingSetting = await _context.AppSettings
                        .FirstOrDefaultAsync(s => s.Key == key && s.Category == category);
                    
                    if (existingSetting != null)
                    {
                        previousSettings[key] = existingSetting.Value;
                    }
                }
                
                return System.Text.Json.JsonSerializer.Serialize(previousSettings);
            }
            catch
            {
                return "{}";
            }
        }
    }

    public class UpdateInfo
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public DateTime LastUpdateCheck { get; set; }
        public List<string> UpdateNotes { get; set; } = new();
        public bool CriticalUpdate { get; set; }
    }
}
