using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Security;

namespace UmiHealthPOS.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAccessAsync(AccessLog accessLog)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    UserId = accessLog.UserId,
                    Type = $"{accessLog.Action}_{accessLog.Controller}".ToUpper(),
                    Description = $"{accessLog.Action} {accessLog.Controller}",
                    Status = accessLog.Status,
                    IpAddress = accessLog.IpAddress,
                    UserAgent = accessLog.UserAgent,
                    CreatedAt = accessLog.Timestamp,
                    Details = accessLog.MetadataJson
                };

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Access logged: {Action} {Controller} by user {UserId} at {Timestamp}",
                    accessLog.Action, accessLog.Controller, accessLog.UserId, accessLog.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log access for user {UserId}", accessLog.UserId);
            }
        }

        public async Task LogSecurityEventAsync(SecurityEventLog securityEvent)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    UserId = securityEvent.UserId,
                    Type = $"SECURITY_{securityEvent.EventType}".ToUpper(),
                    Description = securityEvent.Description,
                    Status = securityEvent.Success ? "success" : "failed",
                    IpAddress = securityEvent.IpAddress,
                    UserAgent = securityEvent.UserAgent,
                    CreatedAt = securityEvent.Timestamp,
                    Details = securityEvent.Details
                };

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Security event logged: {EventType} by user {UserId} at {Timestamp}",
                    securityEvent.EventType, securityEvent.UserId, securityEvent.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event for user {UserId}", securityEvent.UserId);
            }
        }

        public async Task LogSystemEventAsync(SystemEventLog systemEvent)
        {
            try
            {
                var activityLog = new ActivityLog
                {
                    Type = $"SYSTEM_{systemEvent.EventType}".ToUpper(),
                    Description = $"{systemEvent.Component}: {systemEvent.Description}",
                    Status = systemEvent.Level.ToLower(),
                    CreatedAt = systemEvent.Timestamp,
                    Details = systemEvent.Details
                };

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("System event logged: {EventType} in {Component} at {Timestamp}",
                    systemEvent.EventType, systemEvent.Component, systemEvent.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system event for component {Component}", systemEvent.Component);
            }
        }

        public async Task<List<AccessLog>> GetAccessLogsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            try
            {
                var query = _context.ActivityLogs.AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(al => al.UserId == userId);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= endDate.Value);
                }

                var queryData = await query
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(limit)
                    .Select(al => new { al, Type = al.Type })
                    .ToListAsync();

                var logs = queryData
                    .Select(x => {
                        var typeParts = x.Type.Contains("_") ? x.Type.Split('_') : new[] { x.Type };
                        return new AccessLog
                        {
                            UserId = x.al.UserId ?? string.Empty,
                            Action = typeParts.Length > 0 ? typeParts[0] : "UNKNOWN",
                            Controller = typeParts.Length > 1 ? typeParts[1] : "UNKNOWN",
                            HttpMethod = "GET",
                            Status = "Success",
                            IpAddress = x.al.IpAddress ?? string.Empty,
                            UserAgent = x.al.UserAgent ?? string.Empty,
                            Timestamp = x.al.CreatedAt,
                            MetadataJson = x.al.Details ?? string.Empty,
                            Role = string.Empty,
                            TenantId = string.Empty,
                            BranchId = null,
                            Resource = string.Empty,
                            ResourceId = string.Empty,
                            Details = string.Empty,
                            IsImpersonated = false,
                            ImpersonatedByUserId = string.Empty
                        };
                    })
                    .ToList();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get access logs for user {UserId}", userId);
                return new List<AccessLog>();
            }
        }

        public async Task<List<SecurityEventLog>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            try
            {
                var query = _context.ActivityLogs
                    .Where(al => al.Type.StartsWith("SECURITY_"));

                if (startDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= endDate.Value);
                }

                var queryData = await query
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(limit)
                    .Select(al => new { al, EventType = al.Type.Replace("SECURITY_", "") })
                    .ToListAsync();

                var events = queryData
                    .Select(x => new SecurityEventLog
                    {
                        UserId = x.al.UserId,
                        EventType = x.EventType,
                        Description = x.al.Description,
                        IpAddress = x.al.IpAddress,
                        UserAgent = x.al.UserAgent,
                        Timestamp = x.al.CreatedAt,
                        Success = x.al.Status == "success",
                        Details = x.al.Details
                    })
                    .ToList();

                // Log security event
                var securityEvent = new SecurityEventLog
                {
                    UserId = "UserId",
                    EventType = "SUSPICIOUS_ACTIVITY",
                    Description = "Description",
                    IpAddress = "IpAddress",
                    UserAgent = "UserAgent",
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    Details = "Details"
                };

                await LogSecurityEventAsync(securityEvent);

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get security events");
                return new List<SecurityEventLog>();
            }
        }

        public async Task<List<SystemEventLog>> GetSystemEventsAsync(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            try
            {
                var query = _context.ActivityLogs
                    .Where(al => al.Type.StartsWith("SYSTEM_"));

                if (startDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= endDate.Value);
                }

                                var queryData = await query
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(limit)
                    .Select(al => new { al, Type = al.Type })
                    .ToListAsync();

                var events = queryData
                    .AsEnumerable()
                    .Select(x => {
                        var eventType = x.Type.Replace("SYSTEM_", "");
                        var typeParts = x.Type.Split('_');
                        return new SystemEventLog
                        {
                            EventType = eventType,
                            Description = x.al.Description,
                            Component = typeParts.Length > 1 ? string.Join("_", typeParts.Skip(1)) : "UNKNOWN",
                            Timestamp = x.al.CreatedAt,
                            Details = x.al.Details,
                            Level = x.al.Status
                        };
                    })
                    .ToList();

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system events");
                return new List<SystemEventLog>();
            }
        }

        public async Task CleanupOldLogsAsync(int retentionDays = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                
                var oldLogs = _context.ActivityLogs
                    .Where(al => al.CreatedAt < cutoffDate)
                    .ToList();

                if (oldLogs.Any())
                {
                    _context.ActivityLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cleaned up {Count} old activity logs older than {Days} days",
                        oldLogs.Count, retentionDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old activity logs");
            }
        }
    }
}
