using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System.Data.Common;
using Npgsql;
using System.Text.Json;

namespace UmiHealthPOS.Services
{
    public interface IBackupService
    {
        Task<string> CreateBackupAsync();
        Task<byte[]> GetLatestBackupAsync();
        Task<List<BackupInfo>> GetBackupHistoryAsync();
        Task<bool> DeleteBackupAsync(string backupId);
    }

    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public BackupService(ILogger<BackupService> logger, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        public async Task<string> CreateBackupAsync()
        {
            try
            {
                var backupSettings = await GetBackupSettingsAsync();
                var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                var backupPath = Path.Combine(backupSettings.BackupLocation, $"{backupId}.zip");

                // Ensure backup directory exists
                Directory.CreateDirectory(backupSettings.BackupLocation);

                // Create backup file
                using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
                {
                    // Backup database data
                    await BackupDatabaseDataAsync(archive);

                    // Backup configuration files
                    await BackupConfigurationFilesAsync(archive);

                    // Backup logs
                    await BackupLogFilesAsync(archive);
                }

                // Record backup in database
                await RecordBackupAsync(backupId, backupPath);

                _logger.LogInformation("Backup created successfully: {BackupId}", backupId);
                return backupId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup");
                throw;
            }
        }

        public async Task<byte[]> GetLatestBackupAsync()
        {
            try
            {
                var latestBackup = await GetLatestBackupRecordAsync();
                if (latestBackup == null)
                {
                    throw new FileNotFoundException("No backup found");
                }

                if (!File.Exists(latestBackup.FilePath))
                {
                    throw new FileNotFoundException($"Backup file not found: {latestBackup.FilePath}");
                }

                return await File.ReadAllBytesAsync(latestBackup.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest backup");
                throw;
            }
        }

        public async Task<List<BackupInfo>> GetBackupHistoryAsync()
        {
            try
            {
                var backups = await _context.SystemSettings
                    .Where(s => s.Category == "backup" && s.Key.StartsWith("backup_"))
                    .OrderByDescending(s => s.UpdatedAt)
                    .ToListAsync();

                var backupList = new List<BackupInfo>();

                foreach (var backup in backups)
                {
                    var backupData = System.Text.Json.JsonSerializer.Deserialize<BackupRecord>(backup.Value ?? "{}");
                    if (backupData != null)
                    {
                        backupList.Add(new BackupInfo
                        {
                            Id = backup.Key,
                            CreatedAt = backup.UpdatedAt,
                            FileSize = backupData.FileSize,
                            FilePath = backupData.FilePath,
                            Status = backupData.Status
                        });
                    }
                }

                return backupList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get backup history");
                return new List<BackupInfo>();
            }
        }

        public async Task<bool> DeleteBackupAsync(string backupId)
        {
            try
            {
                var backup = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == backupId && s.Category == "backup");

                if (backup == null)
                {
                    return false;
                }

                var backupData = System.Text.Json.JsonSerializer.Deserialize<BackupRecord>(backup.Value ?? "{}");
                if (backupData != null && File.Exists(backupData.FilePath))
                {
                    File.Delete(backupData.FilePath);
                }

                _context.SystemSettings.Remove(backup);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Backup deleted: {BackupId}", backupId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup: {BackupId}", backupId);
                return false;
            }
        }

        private async Task BackupDatabaseDataAsync(ZipArchive archive)
        {
            try
            {
                // Export all important tables to JSON files
                var tables = new[]
                {
                    "Tenants", "Users", "Products", "Customers", "Sales", "InventoryItems",
                    "Suppliers", "Patients", "Prescriptions", "Employees", "Categories"
                };

                foreach (var table in tables)
                {
                    var entry = archive.CreateEntry($"database/{table.ToLower()}.json");

                    using var writer = new StreamWriter(entry.Open());
                    var data = await ExportTableToJsonAsync(table);
                    await writer.WriteAsync(data);
                }

                // Generate database schema
                await GenerateDatabaseSchemaAsync(archive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup database data");
                throw;
            }
        }

        private async Task BackupConfigurationFilesAsync(ZipArchive archive)
        {
            try
            {
                var configFiles = new[]
                {
                    "appsettings.json",
                    "appsettings.Development.json",
                    "appsettings.Production.json"
                };

                foreach (var file in configFiles)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), file);
                    if (File.Exists(filePath))
                    {
                        var entry = archive.CreateEntry($"config/{file}");
                        using var fileStream = File.OpenRead(filePath);
                        using var entryStream = entry.Open();
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup configuration files");
                throw;
            }
        }

        private async Task BackupLogFilesAsync(ZipArchive archive)
        {
            try
            {
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (Directory.Exists(logDirectory))
                {
                    var logFiles = Directory.GetFiles(logDirectory, "*.log")
                        .Where(f => File.GetLastWriteTime(f) > DateTime.UtcNow.AddDays(-30));

                    foreach (var logFile in logFiles)
                    {
                        var fileName = Path.GetFileName(logFile);
                        var entry = archive.CreateEntry($"logs/{fileName}");
                        using var fileStream = File.OpenRead(logFile);
                        using var entryStream = entry.Open();
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup log files");
                throw;
            }
        }

        private async Task<string> ExportTableToJsonAsync(string tableName)
        {
            try
            {
                // Use reflection to get the DbSet dynamically
                var dbSetProperty = typeof(ApplicationDbContext).GetProperty(tableName);
                if (dbSetProperty == null)
                {
                    return "[]";
                }

                var dbSet = dbSetProperty.GetValue(_context);
                if (dbSet == null)
                {
                    return "[]";
                }

                var data = await Task.Run(() =>
                {
                    var query = System.Linq.Expressions.Expression.Call(
                        System.Linq.Expressions.Expression.Constant(dbSet),
                        typeof(System.Linq.IQueryable).GetMethod("ToList"));
                    return System.Linq.Expressions.Expression.Lambda<Func<object>>(query).Compile()();
                });

                return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export table {TableName}", tableName);
                return "[]";
            }
        }

        private async Task GenerateDatabaseSchemaAsync(ZipArchive archive)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var schema = new DatabaseSchema
                {
                    GeneratedAt = DateTime.UtcNow,
                    DatabaseType = "PostgreSQL",
                    Version = "14+",
                    Tables = new List<TableSchema>()
                };

                // Get all table information
                var tables = await GetTableSchemasAsync(connection);
                schema.Tables.AddRange(tables);

                // Add foreign key relationships
                var foreignKeys = await GetForeignKeySchemasAsync(connection);
                foreach (var table in schema.Tables)
                {
                    table.ForeignKeys = foreignKeys.Where(fk => fk.TableName == table.Name).ToList();
                }

                // Serialize schema to JSON
                var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var schemaEntry = archive.CreateEntry("database/schema.json");
                using var writer = new StreamWriter(schemaEntry.Open());
                await writer.WriteAsync(schemaJson);

                _logger.LogInformation("Database schema generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate database schema");
                throw;
            }
        }

        private async Task<List<TableSchema>> GetTableSchemasAsync(DbConnection connection)
        {
            var tables = new List<TableSchema>();
            
            // Get table names
            var tableNames = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name";
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }

            // Get column information for each table
            foreach (var tableName in tableNames)
            {
                var tableSchema = new TableSchema
                {
                    Name = tableName,
                    Columns = new List<ColumnSchema>()
                };

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT 
                            column_name,
                            data_type,
                            is_nullable,
                            column_default,
                            character_maximum_length,
                            numeric_precision,
                            numeric_scale
                        FROM information_schema.columns 
                        WHERE table_schema = 'public' 
                        AND table_name = @table_name
                        ORDER BY ordinal_position";
                    
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@table_name";
                    parameter.Value = tableName;
                    command.Parameters.Add(parameter);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var column = new ColumnSchema
                        {
                            Name = reader.GetString(0),
                            DataType = reader.GetString(1),
                            IsNullable = reader.GetString(2) == "YES",
                            DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                            MaxLength = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                            Precision = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                            Scale = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                        };
                        tableSchema.Columns.Add(column);
                    }
                }

                // Get primary key information
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT kcu.column_name
                        FROM information_schema.table_constraints tc
                        JOIN information_schema.key_column_usage kcu 
                            ON tc.constraint_name = kcu.constraint_name
                            AND tc.table_schema = kcu.table_schema
                        WHERE tc.constraint_type = 'PRIMARY KEY'
                        AND tc.table_schema = 'public'
                        AND tc.table_name = @table_name
                        ORDER BY kcu.ordinal_position";
                    
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@table_name";
                    parameter.Value = tableName;
                    command.Parameters.Add(parameter);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tableSchema.PrimaryKeyColumns.Add(reader.GetString(0));
                    }
                }

                tables.Add(tableSchema);
            }

            return tables;
        }

        private async Task<List<ForeignKeySchema>> GetForeignKeySchemasAsync(DbConnection connection)
        {
            var foreignKeys = new List<ForeignKeySchema>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        tc.table_name,
                        kcu.column_name,
                        ccu.table_name AS foreign_table_name,
                        ccu.column_name AS foreign_column_name,
                        tc.constraint_name
                    FROM information_schema.table_constraints AS tc 
                    JOIN information_schema.key_column_usage AS kcu
                        ON tc.constraint_name = kcu.constraint_name
                        AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage AS ccu
                        ON ccu.constraint_name = tc.constraint_name
                        AND ccu.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'FOREIGN KEY'
                    AND tc.table_schema = 'public'";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var foreignKey = new ForeignKeySchema
                    {
                        TableName = reader.GetString(0),
                        ColumnName = reader.GetString(1),
                        ReferencedTableName = reader.GetString(2),
                        ReferencedColumnName = reader.GetString(3),
                        ConstraintName = reader.GetString(4)
                    };
                    foreignKeys.Add(foreignKey);
                }
            }

            return foreignKeys;
        }

        private async Task RecordBackupAsync(string backupId, string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var backupRecord = new BackupRecord
                {
                    BackupId = backupId,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow
                };

                var setting = new SystemSetting
                {
                    Key = backupId,
                    Value = System.Text.Json.JsonSerializer.Serialize(backupRecord),
                    Category = "backup",
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SystemSettings.Add(setting);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record backup: {BackupId}", backupId);
                throw;
            }
        }

        private async Task<BackupRecord?> GetLatestBackupRecordAsync()
        {
            try
            {
                var latestBackup = await _context.SystemSettings
                    .Where(s => s.Category == "backup" && s.Key.StartsWith("backup_"))
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (latestBackup == null)
                {
                    return null;
                }

                return System.Text.Json.JsonSerializer.Deserialize<BackupRecord>(latestBackup.Value ?? "{}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest backup record");
                return null;
            }
        }

        private async Task<BackupSettings> GetBackupSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "backup")
                .ToListAsync();

            return new BackupSettings
            {
                BackupLocation = GetSettingValue(settings, "backupLocation") ?? Path.Combine(Directory.GetCurrentDirectory(), "backups"),
                BackupRetention = int.Parse(GetSettingValue(settings, "backupRetention") ?? "30"),
                AutoBackup = bool.Parse(GetSettingValue(settings, "autoBackup") ?? "true"),
                BackupFrequency = GetSettingValue(settings, "backupFrequency") ?? "Daily"
            };
        }

        private string? GetSettingValue(List<AppSetting> settings, string key)
        {
            return settings.FirstOrDefault(s => s.Key == key)?.Value;
        }
    }

    public class BackupSettings
    {
        public string BackupLocation { get; set; } = "";
        public int BackupRetention { get; set; } = 30;
        public bool AutoBackup { get; set; } = true;
        public string BackupFrequency { get; set; } = "Daily";
    }

    public class BackupRecord
    {
        public string BackupId { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class BackupInfo
    {
        public string Id { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
