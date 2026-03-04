using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealthPOS.Attributes;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Controllers.Api
{
    /// <summary>
    /// Controller for database and cache management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [RequirePermission("SYSTEM_ADMIN")]
    public class SystemMaintenanceController : ControllerBase
    {
        private readonly DatabaseCleanupService _cleanupService;
        private readonly ILogger<SystemMaintenanceController> _logger;

        public SystemMaintenanceController(
            DatabaseCleanupService cleanupService,
            ILogger<SystemMaintenanceController> logger)
        {
            _cleanupService = cleanupService;
            _logger = logger;
        }

        /// <summary>
        /// Clears all transactional data from the database
        /// </summary>
        [HttpPost("clear-transactional-data")]
        public async Task<ActionResult<DatabaseCleanupResult>> ClearTransactionalData()
        {
            try
            {
                _logger.LogInformation("Clear transactional data requested by user");
                
                var result = await _cleanupService.ClearTransactionalDataAsync();
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = "Transactional data cleared successfully",
                        result = result
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = "Failed to clear transactional data",
                        error = result.ErrorMessage,
                        result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing transactional data");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clears all user session data
        /// </summary>
        [HttpPost("clear-sessions")]
        public async Task<ActionResult<DatabaseCleanupResult>> ClearSessionData()
        {
            try
            {
                _logger.LogInformation("Clear session data requested by user");
                
                var result = await _cleanupService.ClearSessionDataAsync();
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = "Session data cleared successfully",
                        result = result
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = "Failed to clear session data",
                        error = result.ErrorMessage,
                        result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing session data");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clears all activity logs
        /// </summary>
        [HttpPost("clear-activity-logs")]
        public async Task<ActionResult<DatabaseCleanupResult>> ClearActivityLogs()
        {
            try
            {
                _logger.LogInformation("Clear activity logs requested by user");
                
                var result = await _cleanupService.ClearActivityLogsAsync();
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = "Activity logs cleared successfully",
                        result = result
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = "Failed to clear activity logs",
                        error = result.ErrorMessage,
                        result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing activity logs");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clears all cache memory
        /// </summary>
        [HttpPost("clear-cache")]
        public ActionResult<CacheCleanupResult> ClearCacheMemory()
        {
            try
            {
                _logger.LogInformation("Clear cache memory requested by user");
                
                var result = _cleanupService.ClearCacheMemory();
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = "Cache memory cleared successfully",
                        result = result
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = "Failed to clear cache memory",
                        error = result.ErrorMessage,
                        result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache memory");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clears cache entries matching a pattern
        /// </summary>
        [HttpPost("clear-cache-by-pattern")]
        public ActionResult<CacheCleanupResult> ClearCacheByPattern([FromBody] CachePatternRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Pattern))
                {
                    return BadRequest(new { message = "Pattern is required" });
                }

                _logger.LogInformation("Clear cache by pattern requested: {Pattern}", request.Pattern);
                
                var result = _cleanupService.ClearCacheByPattern(request.Pattern);
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = $"Cache entries matching '{request.Pattern}' cleared successfully",
                        result = result
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = $"Failed to clear cache entries matching '{request.Pattern}'",
                        error = result.ErrorMessage,
                        result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache by pattern");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Performs complete system cleanup (database + cache)
        /// </summary>
        [HttpPost("full-cleanup")]
        public async Task<ActionResult<SystemCleanupResult>> PerformFullCleanup()
        {
            try
            {
                _logger.LogInformation("Full system cleanup requested by user");
                
                var result = await _cleanupService.PerformFullCleanupAsync();
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = "Full system cleanup completed successfully",
                        result = result
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        message = "Full system cleanup completed with errors",
                        error = result.ErrorMessage,
                        result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing full system cleanup");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets current database statistics
        /// </summary>
        [HttpGet("database-statistics")]
        public async Task<ActionResult<DatabaseStatistics>> GetDatabaseStatistics()
        {
            try
            {
                _logger.LogInformation("Database statistics requested by user");
                
                var stats = await _cleanupService.GetDatabaseStatisticsAsync();
                
                return Ok(new { 
                    message = "Database statistics retrieved successfully",
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database statistics");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets system health status
        /// </summary>
        [HttpGet("health-status")]
        public async Task<ActionResult<SystemHealthStatus>> GetSystemHealthStatus()
        {
            try
            {
                _logger.LogInformation("System health status requested by user");
                
                var dbStats = await _cleanupService.GetDatabaseStatisticsAsync();
                
                var healthStatus = new SystemHealthStatus
                {
                    Timestamp = DateTime.UtcNow,
                    DatabaseStatus = "Healthy",
                    CacheStatus = "Healthy",
                    TotalRecords = dbStats.SalesCount + dbStats.SaleItemsCount + 
                                   dbStats.PrescriptionsCount + dbStats.PatientsCount + 
                                   dbStats.InventoryItemsCount + dbStats.UsersCount,
                    ActiveSessions = dbStats.UserSessionsCount,
                    LastCleanup = null // This could be tracked in a real implementation
                };

                return Ok(new { 
                    message = "System health status retrieved successfully",
                    status = healthStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health status");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }
    }

    #region Request DTOs

    public class CachePatternRequest
    {
        public string Pattern { get; set; } = string.Empty;
    }

    #endregion

    #region Response DTOs

    public class SystemHealthStatus
    {
        public DateTime Timestamp { get; set; }
        public string DatabaseStatus { get; set; } = string.Empty;
        public string CacheStatus { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int ActiveSessions { get; set; }
        public DateTime? LastCleanup { get; set; }
    }

    #endregion
}
