using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace UmiHealthPOS.Services
{
    public interface ISessionTimeoutService
    {
        Task<int> GetUserSessionTimeoutAsync(string userId, string tenantId);
        Task UpdateUserLastActivityAsync(string userId, string tenantId);
        Task<bool> IsSessionExpiredAsync(string userId, string tenantId, string token);
        Task ExtendSessionAsync(string userId, string tenantId, string token);
    }

    public class SessionTimeoutService : ISessionTimeoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SessionTimeoutService> _logger;

        public SessionTimeoutService(ApplicationDbContext context, ILogger<SessionTimeoutService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GetUserSessionTimeoutAsync(string userId, string tenantId)
        {
            try
            {
                // Default timeout is 30 minutes
                var defaultTimeout = 30;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return defaultTimeout;
                }

                // Try to get user-specific timeout from PharmacistProfile
                var pharmacistProfile = await _context.PharmacistProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId && p.IsActive);

                if (pharmacistProfile != null)
                {
                    return pharmacistProfile.SessionTimeout > 0 ? pharmacistProfile.SessionTimeout : defaultTimeout;
                }

                // Try to get from UserAccount (for other roles)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenantId);

                // For non-pharmacist users, you could add session timeout to UserAccount
                // For now, return default timeout
                return defaultTimeout;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user session timeout for user {UserId}", userId);
                return 30; // Return default timeout on error
            }
        }

        public async Task UpdateUserLastActivityAsync(string userId, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return;
                }

                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId && s.IsActive);

                if (session != null)
                {
                    session.UpdatedAt = DateTime.UtcNow;

                    // Extend session expiry based on user's timeout setting
                    var timeoutMinutes = await GetUserSessionTimeoutAsync(userId, tenantId);
                    session.ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Updated last activity for user {UserId}, session expires at {ExpiresAt}",
                        userId, session.ExpiresAt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user last activity for user {UserId}", userId);
            }
        }

        public async Task<bool> IsSessionExpiredAsync(string userId, string tenantId, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(token))
                {
                    return true;
                }

                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId && s.Token == token && s.IsActive);

                if (session == null)
                {
                    return true; // No session found
                }

                return DateTime.UtcNow > session.ExpiresAt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session expiration for user {UserId}", userId);
                return true; // Assume expired on error
            }
        }

        public async Task ExtendSessionAsync(string userId, string tenantId, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(token))
                {
                    return;
                }

                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId && s.Token == token && s.IsActive);

                if (session != null)
                {
                    var timeoutMinutes = await GetUserSessionTimeoutAsync(userId, tenantId);
                    session.UpdatedAt = DateTime.UtcNow;
                    session.ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
                    await _context.SaveChangesAsync();

                    _logger.LogDebug("Extended session for user {UserId} by {TimeoutMinutes} minutes",
                        userId, timeoutMinutes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending session for user {UserId}", userId);
            }
        }
    }
}
