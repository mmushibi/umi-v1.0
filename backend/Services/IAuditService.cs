using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface IAuditService
    {
        Task LogAccessAsync(AccessLog accessLog);
        Task LogSecurityEventAsync(SecurityEventLog securityEvent);
        Task LogSystemEventAsync(SystemEventLog systemEvent);
        Task<List<AccessLog>> GetAccessLogsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        Task<List<SecurityEventLog>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        Task<List<SystemEventLog>> GetSystemEventsAsync(DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        Task CleanupOldLogsAsync(int retentionDays = 90);
    }

    public class SecurityEventLog
    {
        public string UserId { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string Details { get; set; }
        public string TenantId { get; set; }
    }

    public class SystemEventLog
    {
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Component { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
        public string Level { get; set; } // Info, Warning, Error, Critical
    }
}
