using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Services
{
    /// <summary>
    /// Service for clearing database data and cache memory
    /// </summary>
    public class DatabaseCleanupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<DatabaseCleanupService> _logger;

        public DatabaseCleanupService(
            ApplicationDbContext context,
            IMemoryCache memoryCache,
            ILogger<DatabaseCleanupService> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// Clears all transactional data from the database
        /// </summary>
        public async Task<DatabaseCleanupResult> ClearTransactionalDataAsync()
        {
            var result = new DatabaseCleanupResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting database cleanup for transactional data");

                // Clear in proper order to respect foreign key constraints
                var tables = new[]
                {
                    nameof(ApplicationDbContext.SaleItems),
                    nameof(ApplicationDbContext.Sales),
                    nameof(ApplicationDbContext.StockTransactions),
                    nameof(ApplicationDbContext.PrescriptionItems),
                    nameof(ApplicationDbContext.Prescriptions),
                    nameof(ApplicationDbContext.DaybookTransactionItems),
                    nameof(ApplicationDbContext.DaybookTransactions),
                    nameof(ApplicationDbContext.Invoices),
                    nameof(ApplicationDbContext.CreditNotes),
                    nameof(ApplicationDbContext.Payments),
                    nameof(ApplicationDbContext.ControlledSubstanceAudits),
                    nameof(ApplicationDbContext.ShiftAssignments),
                    nameof(ApplicationDbContext.Shifts)
                };

                foreach (var tableName in tables)
                {
                    var count = await ClearTableAsync(tableName);
                    result.TablesCleared.Add(tableName, count);
                    _logger.LogInformation("Cleared {Count} records from {TableName}", count, tableName);
                }

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Database cleanup completed successfully in {Duration}ms", 
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Clears all user session data
        /// </summary>
        public async Task<DatabaseCleanupResult> ClearSessionDataAsync()
        {
            var result = new DatabaseCleanupResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting session data cleanup");

                var sessionCount = await ClearTableAsync(nameof(ApplicationDbContext.UserSessions));
                result.TablesCleared.Add(nameof(ApplicationDbContext.UserSessions), sessionCount);

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Session cleanup completed. Cleared {Count} sessions", sessionCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Clears all activity logs
        /// </summary>
        public async Task<DatabaseCleanupResult> ClearActivityLogsAsync()
        {
            var result = new DatabaseCleanupResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting activity logs cleanup");

                var logCount = await ClearTableAsync(nameof(ApplicationDbContext.ActivityLogs));
                result.TablesCleared.Add(nameof(ApplicationDbContext.ActivityLogs), logCount);

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Activity logs cleanup completed. Cleared {Count} logs", logCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during activity logs cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Clears all cache memory
        /// </summary>
        public CacheCleanupResult ClearCacheMemory()
        {
            var result = new CacheCleanupResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting cache memory cleanup");

                // Get all cache keys before clearing (for reporting)
                var cacheKeys = GetCacheKeys();
                result.KeysCleared = cacheKeys.Count;

                // Clear all cache entries
                if (_memoryCache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0); // Compact 100% of cache
                }

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Cache cleanup completed. Cleared {Count} cache entries in {Duration}ms", 
                    result.KeysCleared, result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Clears specific cache entries by pattern
        /// </summary>
        public CacheCleanupResult ClearCacheByPattern(string pattern)
        {
            var result = new CacheCleanupResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting pattern-based cache cleanup for pattern: {Pattern}", pattern);

                var cacheKeys = GetCacheKeys();
                var matchingKeys = cacheKeys.Where(key => key.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
                
                result.KeysCleared = matchingKeys.Count;

                // Remove matching cache entries
                foreach (var key in matchingKeys)
                {
                    _memoryCache.Remove(key);
                }

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Pattern-based cache cleanup completed. Cleared {Count} entries for pattern '{Pattern}'", 
                    result.KeysCleared, pattern);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during pattern-based cache cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Performs complete system cleanup (database + cache)
        /// </summary>
        public async Task<SystemCleanupResult> PerformFullCleanupAsync()
        {
            var result = new SystemCleanupResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting full system cleanup");

                // Clear transactional data
                result.DatabaseResult = await ClearTransactionalDataAsync();
                
                // Clear session data
                result.SessionResult = await ClearSessionDataAsync();
                
                // Clear activity logs
                result.ActivityLogsResult = await ClearActivityLogsAsync();
                
                // Clear cache memory
                result.CacheResult = ClearCacheMemory();

                result.Success = result.DatabaseResult.Success && 
                              result.SessionResult.Success && 
                              result.ActivityLogsResult.Success && 
                              result.CacheResult.Success;

                result.Duration = DateTime.UtcNow - startTime;
                
                if (result.Success)
                {
                    _logger.LogInformation("Full system cleanup completed successfully in {Duration}ms", 
                        result.Duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("Full system cleanup completed with some errors in {Duration}ms", 
                        result.Duration.TotalMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during full system cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Gets database statistics
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            try
            {
                var stats = new DatabaseStatistics
                {
                    GeneratedAt = DateTime.UtcNow
                };

                // Get record counts for each table
                stats.SalesCount = await _context.Sales.CountAsync();
                stats.SaleItemsCount = await _context.SaleItems.CountAsync();
                stats.PrescriptionsCount = await _context.Prescriptions.CountAsync();
                stats.PatientsCount = await _context.Patients.CountAsync();
                stats.InventoryItemsCount = await _context.InventoryItems.CountAsync();
                stats.UsersCount = await _context.Users.CountAsync();
                stats.ActivityLogsCount = await _context.ActivityLogs.CountAsync();
                stats.UserSessionsCount = await _context.UserSessions.CountAsync();

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database statistics");
                return new DatabaseStatistics { GeneratedAt = DateTime.UtcNow };
            }
        }

        private async Task<int> ClearTableAsync(string tableName)
        {
            try
            {
                // Use Entity Framework to delete all records safely
                switch (tableName)
                {
                    case nameof(ApplicationDbContext.SaleItems):
                        var saleItems = await _context.SaleItems.ToListAsync();
                        _context.SaleItems.RemoveRange(saleItems);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.Sales):
                        var sales = await _context.Sales.ToListAsync();
                        _context.Sales.RemoveRange(sales);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.StockTransactions):
                        var stockTransactions = await _context.StockTransactions.ToListAsync();
                        _context.StockTransactions.RemoveRange(stockTransactions);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.PrescriptionItems):
                        var prescriptionItems = await _context.PrescriptionItems.ToListAsync();
                        _context.PrescriptionItems.RemoveRange(prescriptionItems);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.Prescriptions):
                        var prescriptions = await _context.Prescriptions.ToListAsync();
                        _context.Prescriptions.RemoveRange(prescriptions);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.DaybookTransactionItems):
                        var daybookItems = await _context.DaybookTransactionItems.ToListAsync();
                        _context.DaybookTransactionItems.RemoveRange(daybookItems);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.DaybookTransactions):
                        var daybookTransactions = await _context.DaybookTransactions.ToListAsync();
                        _context.DaybookTransactions.RemoveRange(daybookTransactions);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.Invoices):
                        var invoices = await _context.Invoices.ToListAsync();
                        _context.Invoices.RemoveRange(invoices);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.CreditNotes):
                        var creditNotes = await _context.CreditNotes.ToListAsync();
                        _context.CreditNotes.RemoveRange(creditNotes);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.Payments):
                        var payments = await _context.Payments.ToListAsync();
                        _context.Payments.RemoveRange(payments);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.ControlledSubstanceAudits):
                        var audits = await _context.ControlledSubstanceAudits.ToListAsync();
                        _context.ControlledSubstanceAudits.RemoveRange(audits);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.ShiftAssignments):
                        var shiftAssignments = await _context.ShiftAssignments.ToListAsync();
                        _context.ShiftAssignments.RemoveRange(shiftAssignments);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.Shifts):
                        var shifts = await _context.Shifts.ToListAsync();
                        _context.Shifts.RemoveRange(shifts);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.UserSessions):
                        var sessions = await _context.UserSessions.ToListAsync();
                        _context.UserSessions.RemoveRange(sessions);
                        return await _context.SaveChangesAsync();
                    
                    case nameof(ApplicationDbContext.ActivityLogs):
                        var logs = await _context.ActivityLogs.ToListAsync();
                        _context.ActivityLogs.RemoveRange(logs);
                        return await _context.SaveChangesAsync();
                    
                    default:
                        _logger.LogWarning("Unknown table name: {TableName}", tableName);
                        return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing table {TableName}", tableName);
                throw;
            }
        }

        private List<string> GetCacheKeys()
        {
            // This is a limitation of IMemoryCache - we can't easily enumerate all keys
            // For now, we'll return common cache keys used in the system
            return new List<string>
            {
                "UserPermissions",
                "TenantSettings",
                "BranchData",
                "InventoryCache",
                "ProductCache",
                "CustomerCache",
                "PrescriptionCache",
                "SearchCache",
                "ReportCache",
                "SessionCache",
                "SecurityContext",
                "RolePermissions",
                "NavigationMenu",
                "DashboardData"
            };
        }
    }

    #region Result Classes

    public class DatabaseCleanupResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, int> TablesCleared { get; set; } = new();
        public int TotalRecordsCleared => TablesCleared.Values.Sum();
    }

    public class CacheCleanupResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public int KeysCleared { get; set; }
    }

    public class SystemCleanupResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public DatabaseCleanupResult DatabaseResult { get; set; } = new();
        public DatabaseCleanupResult SessionResult { get; set; } = new();
        public DatabaseCleanupResult ActivityLogsResult { get; set; } = new();
        public CacheCleanupResult CacheResult { get; set; } = new();
        public int TotalRecordsCleared => DatabaseResult.TotalRecordsCleared + 
                                       SessionResult.TotalRecordsCleared + 
                                       ActivityLogsResult.TotalRecordsCleared;
    }

    public class DatabaseStatistics
    {
        public DateTime GeneratedAt { get; set; }
        public int SalesCount { get; set; }
        public int SaleItemsCount { get; set; }
        public int PrescriptionsCount { get; set; }
        public int PatientsCount { get; set; }
        public int InventoryItemsCount { get; set; }
        public int UsersCount { get; set; }
        public int ActivityLogsCount { get; set; }
        public int UserSessionsCount { get; set; }
    }

    #endregion
}
