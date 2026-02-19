using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public class SettingsDataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsDataSeeder> _logger;

        public SettingsDataSeeder(ApplicationDbContext context, ILogger<SettingsDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDefaultSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Starting to seed default settings");

                // Check if settings already exist
                var existingSettings = await _context.AppSettings.AnyAsync();
                if (existingSettings)
                {
                    _logger.LogInformation("Settings already exist, skipping seeding");
                    return;
                }

                var defaultSettings = GetDefaultSettings();

                foreach (var setting in defaultSettings)
                {
                    await _context.AppSettings.AddAsync(setting);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded {Count} default settings", defaultSettings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default settings");
                throw;
            }
        }

        private List<AppSetting> GetDefaultSettings()
        {
            return new List<AppSetting>
            {
                // General Settings
                new AppSetting
                {
                    Key = "systemName",
                    Value = "UmiHealth POS",
                    Description = "System name displayed in the application",
                    Category = "general",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "defaultCurrency",
                    Value = "Zambian Kwacha (K)",
                    Description = "Default currency for the system",
                    Category = "general",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "timeZone",
                    Value = "CAT (UTC+2)",
                    Description = "Default time zone for the system",
                    Category = "general",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "companyName",
                    Value = "UmiHealth Solutions",
                    Description = "Company name",
                    Category = "general",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "supportEmail",
                    Value = "support@umihealth.com",
                    Description = "Support email address",
                    Category = "general",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "phoneNumber",
                    Value = "+260 123 456 789",
                    Description = "Company phone number",
                    Category = "general",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "emailNotifications",
                    Value = "true",
                    Description = "Enable email notifications",
                    Category = "general",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "maintenanceMode",
                    Value = "false",
                    Description = "Put system in maintenance mode",
                    Category = "general",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "debugMode",
                    Value = "false",
                    Description = "Enable debug mode",
                    Category = "general",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },

                // Security Settings
                new AppSetting
                {
                    Key = "sessionTimeout",
                    Value = "30",
                    Description = "Session timeout in minutes",
                    Category = "security",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "maxDeviceLimit",
                    Value = "5",
                    Description = "Maximum number of concurrent devices per user account",
                    Category = "security",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "passwordPolicy",
                    Value = "Strong (8+ chars, mixed case)",
                    Description = "Password policy level",
                    Category = "security",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "maxLoginAttempts",
                    Value = "5",
                    Description = "Maximum login attempts before lockout",
                    Category = "security",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "lockoutDuration",
                    Value = "15",
                    Description = "Account lockout duration in minutes",
                    Category = "security",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "passwordExpiry",
                    Value = "90",
                    Description = "Password expiry period in days",
                    Category = "security",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "apiRateLimit",
                    Value = "100",
                    Description = "API rate limit per minute",
                    Category = "security",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "twoFactorAuth",
                    Value = "true",
                    Description = "Enable two-factor authentication",
                    Category = "security",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "apiRateLimiting",
                    Value = "true",
                    Description = "Enable API rate limiting",
                    Category = "security",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "ipWhitelisting",
                    Value = "false",
                    Description = "Enable IP whitelisting",
                    Category = "security",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "forceHttps",
                    Value = "true",
                    Description = "Force HTTPS for all requests",
                    Category = "security",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },

                // Backup Settings
                new AppSetting
                {
                    Key = "backupFrequency",
                    Value = "Daily",
                    Description = "Backup frequency",
                    Category = "backup",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "backupRetention",
                    Value = "30",
                    Description = "Backup retention period in days",
                    Category = "backup",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "backupLocation",
                    Value = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups"),
                    Description = "Backup storage location",
                    Category = "backup",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "compressionLevel",
                    Value = "Standard",
                    Description = "Backup compression level",
                    Category = "backup",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "encryption",
                    Value = "AES-256",
                    Description = "Backup encryption method",
                    Category = "backup",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "cloudStorage",
                    Value = "None",
                    Description = "Cloud storage provider",
                    Category = "backup",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "autoBackup",
                    Value = "true",
                    Description = "Enable automatic backups",
                    Category = "backup",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "backupVerification",
                    Value = "true",
                    Description = "Verify backup integrity",
                    Category = "backup",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "emailBackupReports",
                    Value = "false",
                    Description = "Email backup reports",
                    Category = "backup",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },

                // Email Settings
                new AppSetting
                {
                    Key = "smtpServer",
                    Value = "smtp.gmail.com",
                    Description = "SMTP server address",
                    Category = "email",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "port",
                    Value = "587",
                    Description = "SMTP server port",
                    Category = "email",
                    DataType = "Integer",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "username",
                    Value = "",
                    Description = "SMTP username",
                    Category = "email",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "password",
                    Value = "",
                    Description = "SMTP password",
                    Category = "email",
                    DataType = "String",
                    IsEncrypted = true,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "fromEmail",
                    Value = "noreply@umihealth.com",
                    Description = "From email address",
                    Category = "email",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "fromName",
                    Value = "UmiHealth POS",
                    Description = "From email name",
                    Category = "email",
                    DataType = "String",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "useSslTls",
                    Value = "true",
                    Description = "Use SSL/TLS for SMTP",
                    Category = "email",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                },
                new AppSetting
                {
                    Key = "enableEmailLogging",
                    Value = "true",
                    Description = "Enable email logging",
                    Category = "email",
                    DataType = "Boolean",
                    IsEncrypted = false,
                    IsReadOnly = false,
                    Environment = "Production",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                }
            };
        }
    }
}
