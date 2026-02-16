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
using System.Text.Json;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class AuditLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(
            ApplicationDbContext context,
            ILogger<AuditLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
        {
            try
            {
                var query = _context.ActivityLogs
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.UserId))
                {
                    query = query.Where(a => a.UserId == filter.UserId);
                }

                if (!string.IsNullOrEmpty(filter.TenantId))
                {
                    query = query.Where(a => a.TenantId == filter.TenantId);
                }

                if (!string.IsNullOrEmpty(filter.Action))
                {
                    query = query.Where(a => a.Action == filter.Action);
                }

                // Remove EntityType filter as it doesn't exist in ActivityLog

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= filter.EndDate.Value);
                }

                if (!string.IsNullOrEmpty(filter.Search))
                {
                    query = query.Where(a =>
                        a.Description.Contains(filter.Search) ||
                        (a.Type != null && a.Type.Contains(filter.Search)));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var auditLogs = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(a => new AuditLogDto
                    {
                        Id = a.Id,
                        UserId = a.UserId ?? string.Empty,
                        UserName = "Unknown User", // Would need separate join to get user name
                        TenantId = a.TenantId,
                        TenantName = null, // Not available in ActivityLog
                        Action = a.Action,
                        EntityType = a.Type ?? "Unknown",
                        EntityId = null, // Not available in ActivityLog
                        EntityName = null, // Not available in ActivityLog
                        OldValues = string.Empty, // Not available in ActivityLog
                        NewValues = string.Empty, // Not available in ActivityLog
                        IpAddress = a.IpAddress,
                        UserAgent = a.UserAgent,
                        Description = a.Description,
                        Timestamp = a.CreatedAt,
                        Severity = a.Status ?? "Info",
                        IsSuccess = a.Status == "success"
                    })
                    .ToListAsync();

                return Ok(new PagedResult<AuditLogDto>
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
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<AuditLogStatsDto>> GetAuditLogStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var query = _context.ActivityLogs.AsQueryable();

                var totalLogs = await query.CountAsync();
                var todayLogs = await query.CountAsync(a => a.CreatedAt >= today);
                var thisWeekLogs = await query.CountAsync(a => a.CreatedAt >= weekStart);
                var thisMonthLogs = await query.CountAsync(a => a.CreatedAt >= monthStart);
                var criticalLogs = await query.CountAsync(a => a.Status == "Critical");
                var failedLogs = await query.CountAsync(a => a.Status != "success");
                var successLogs = await query.CountAsync(a => a.Status == "success");

                var actionCounts = await query
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Action, x => x.Count);

                var entityTypeCounts = await query
                    .GroupBy(a => a.Type ?? "Unknown")
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.EntityType, x => x.Count);

                return Ok(new AuditLogStatsDto
                {
                    TotalLogs = totalLogs,
                    TodayLogs = todayLogs,
                    ThisWeekLogs = thisWeekLogs,
                    ThisMonthLogs = thisMonthLogs,
                    CriticalLogs = criticalLogs,
                    FailedLogs = failedLogs,
                    SuccessLogs = successLogs,
                    ActionCounts = actionCounts,
                    EntityTypeCounts = entityTypeCounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogDto>> GetAuditLog(int id)
        {
            try
            {
                var auditLog = await _context.ActivityLogs
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (auditLog == null)
                {
                    return NotFound(new { error = "Audit log not found" });
                }

                return Ok(new AuditLogDto
                {
                    Id = auditLog.Id,
                    UserId = auditLog.UserId ?? string.Empty,
                    UserName = "Unknown User", // Would need separate query to get user name
                    TenantId = auditLog.TenantId,
                    TenantName = null, // Not available in ActivityLog
                    Action = auditLog.Action,
                    EntityType = auditLog.Type ?? "Unknown",
                    EntityId = null, // Not available in ActivityLog
                    EntityName = null, // Not available in ActivityLog
                    OldValues = string.Empty, // Not available in ActivityLog
                    NewValues = string.Empty, // Not available in ActivityLog
                    IpAddress = auditLog.IpAddress,
                    UserAgent = auditLog.UserAgent,
                    Description = auditLog.Description,
                    Timestamp = auditLog.CreatedAt,
                    Severity = auditLog.Status ?? "Info",
                    IsSuccess = auditLog.Status == "success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AuditLogDto>> CreateAuditLog([FromBody] CreateAuditLogRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var auditLog = new ActivityLog
                {
                    UserId = currentUserId,
                    TenantId = request.TenantId,
                    Action = request.Action,
                    Type = request.EntityType ?? "Unknown",
                    Description = request.Description,
                    Category = "General",
                    Status = request.IsSuccess ? "success" : "failed",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                };

                await _context.ActivityLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAuditLog), new { id = auditLog.Id }, new AuditLogDto
                {
                    Id = auditLog.Id,
                    UserId = auditLog.UserId ?? string.Empty,
                    UserName = "Current User",
                    TenantId = auditLog.TenantId,
                    TenantName = null,
                    Action = auditLog.Action,
                    EntityType = auditLog.Type ?? "Unknown",
                    EntityId = null,
                    EntityName = null,
                    OldValues = string.Empty,
                    NewValues = string.Empty,
                    IpAddress = auditLog.IpAddress,
                    UserAgent = auditLog.UserAgent,
                    Description = auditLog.Description,
                    Timestamp = auditLog.CreatedAt,
                    Severity = auditLog.Status ?? "Info",
                    IsSuccess = auditLog.Status == "success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit log");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("cleanup")]
        public async Task<ActionResult> CleanupOldAuditLogs([FromQuery] int daysToKeep = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var logsToDelete = await _context.ActivityLogs
                    .Where(a => a.CreatedAt < cutoffDate)
                    .CountAsync();

                if (logsToDelete > 0)
                {
                    _context.ActivityLogs.RemoveRange(
                        _context.ActivityLogs.Where(a => a.CreatedAt < cutoffDate));
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = $"Cleaned up {logsToDelete} audit logs older than {daysToKeep} days",
                    deletedCount = logsToDelete
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up audit logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportAuditLogs([FromQuery] AuditLogFilterDto filter)
        {
            try
            {
                // Set a large page size for export
                filter.PageSize = 10000;
                filter.Page = 1;

                var result = await GetAuditLogs(filter);
                if (result.Result is OkObjectResult okResult && okResult.Value is PagedResult<AuditLogDto> pagedResult)
                {
                    var csv = GenerateAuditLogCsv(pagedResult.Data);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                    return File(bytes, "text/csv", $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                }

                return StatusCode(500, new { error = "Failed to export audit logs" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private string GenerateAuditLogCsv(List<AuditLogDto> auditLogs)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,User,Tenant,Action,Entity Type,Entity Name,Timestamp,Severity,Status,IP Address,Description");

            foreach (var log in auditLogs)
            {
                csv.AppendLine($"{log.Id}," +
                    $"\"{log.UserName}\"," +
                    $"\"{log.TenantName}\"," +
                    $"{log.Action}," +
                    $"{log.EntityType}," +
                    $"\"{log.EntityName}\"," +
                    $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                    $"{log.Severity}," +
                    $"{(log.IsSuccess ? "Success" : "Failed")}," +
                    $"{log.IpAddress}," +
                    $"\"{log.Description?.Replace("\"", "\"\"")}\"");
            }

            return csv.ToString();
        }

        private string GetClientIpAddress()
        {
            var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ipAddress ?? "Unknown";
        }
    }
}


