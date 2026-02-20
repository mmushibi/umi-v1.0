using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace UmiHealthPOS.Services
{
    public interface ISessionManagementService
    {
        Task<int> GetActiveSessionCountAsync(string userId);
        Task<List<UserSession>> GetActiveSessionsAsync(string userId);
        Task<UserSession> CreateSessionAsync(string userId, string tenantId, string token, string ipAddress, string userAgent);
        Task<bool> TerminateSessionAsync(string sessionId);
        Task<bool> TerminateAllSessionsAsync(string userId);
        Task CleanupExpiredSessionsAsync();
        Task<bool> IsDeviceLimitReachedAsync(string userId, int maxDevices);
    }

    public class SessionManagementService : ISessionManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SessionManagementService> _logger;

        public SessionManagementService(ApplicationDbContext context, ILogger<SessionManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GetActiveSessionCountAsync(string userId)
        {
            try
            {
                return await _context.UserSessions
                    .CountAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active session count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
        {
            try
            {
                return await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
                return new List<UserSession>();
            }
        }

        public async Task<UserSession> CreateSessionAsync(string userId, string tenantId, string token, string ipAddress, string userAgent)
        {
            try
            {
                var session = new UserSession
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Token = token,
                    SessionToken = token,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceInfo = ExtractDeviceInfo(userAgent),
                    Browser = ExtractBrowser(userAgent),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Default 30 minutes
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new session {SessionId} for user {UserId} from {IpAddress}",
                    session.Id, userId, ipAddress);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> TerminateSessionAsync(string sessionId)
        {
            try
            {
                var session = await _context.UserSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.IsActive = false;
                    session.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Terminated session {SessionId}", sessionId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> TerminateAllSessionsAsync(string userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Terminated {Count} sessions for user {UserId}", sessions.Count, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating all sessions for user {UserId}", userId);
                return false;
            }
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            try
            {
                var expiredSessions = await _context.UserSessions
                    .Where(s => s.ExpiresAt <= DateTime.UtcNow || !s.IsActive)
                    .ToListAsync();

                if (expiredSessions.Any())
                {
                    _context.UserSessions.RemoveRange(expiredSessions);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired sessions");
            }
        }

        public async Task<bool> IsDeviceLimitReachedAsync(string userId, int maxDevices)
        {
            try
            {
                var activeCount = await GetActiveSessionCountAsync(userId);
                return activeCount >= maxDevices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking device limit for user {UserId}", userId);
                return false;
            }
        }

        private string ExtractDeviceInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            // Simple device detection - can be enhanced
            if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
                return "Mobile";
            else if (userAgent.Contains("Tablet") || userAgent.Contains("iPad"))
                return "Tablet";
            else
                return "Desktop";
        }

        private string ExtractBrowser(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            if (userAgent.Contains("Chrome"))
                return "Chrome";
            else if (userAgent.Contains("Firefox"))
                return "Firefox";
            else if (userAgent.Contains("Safari"))
                return "Safari";
            else if (userAgent.Contains("Edge"))
                return "Edge";
            else
                return "Other";
        }
    }
}
