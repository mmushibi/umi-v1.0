using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Middleware
{
    /// <summary>
    /// Enhanced session management with timeout warnings and security features
    /// </summary>
    public class EnhancedSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedSessionMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public EnhancedSessionMiddleware(
            RequestDelegate next,
            ILogger<EnhancedSessionMiddleware> logger,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    await ManageUserSessionAsync(context, userId);
                }
            }

            await _next(context);
        }

        private async Task ManageUserSessionAsync(HttpContext context, string userId)
        {
            var sessionKey = $"user_session_{userId}";
            var warningThreshold = _configuration.GetValue<int>("SessionSettings:WarningThresholdMinutes", 5);
            var maxSessionTime = _configuration.GetValue<int>("SessionSettings:MaxSessionMinutes", 30);

            var sessionInfo = await GetSessionInfoAsync(sessionKey);
            
            if (sessionInfo == null)
            {
                // New session
                sessionInfo = new UserSessionInfo
                {
                    UserId = userId,
                    StartTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    WarningSent = false,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString()
                };

                await SetSessionInfoAsync(sessionKey, sessionInfo);
                _logger.LogInformation("New session started for user {UserId} from {IpAddress}", userId, sessionInfo.IpAddress);
            }
            else
            {
                // Update existing session
                sessionInfo.LastActivity = DateTime.UtcNow;
                
                // Check for session timeout warning
                var sessionDuration = DateTime.UtcNow - sessionInfo.StartTime;
                var timeUntilTimeout = TimeSpan.FromMinutes(maxSessionTime) - sessionDuration;

                if (timeUntilTimeout <= TimeSpan.FromMinutes(warningThreshold) && !sessionInfo.WarningSent)
                {
                    await SendSessionWarningAsync(context, sessionInfo, timeUntilTimeout);
                    sessionInfo.WarningSent = true;
                }

                // Check for session timeout
                if (sessionDuration >= TimeSpan.FromMinutes(maxSessionTime))
                {
                    await HandleSessionTimeoutAsync(context, sessionInfo);
                    return;
                }

                // Check for session hijacking (IP address change)
                if (!string.IsNullOrEmpty(sessionInfo.IpAddress) && 
                    sessionInfo.IpAddress != context.Connection.RemoteIpAddress?.ToString())
                {
                    await HandleSuspiciousActivityAsync(context, sessionInfo, "IP address changed");
                }

                await SetSessionInfoAsync(sessionKey, sessionInfo);
            }

            // Add session information to response headers for client-side handling
            context.Response.Headers.Add("X-Session-Timeout-Minutes", maxSessionTime.ToString());
            context.Response.Headers.Add("X-Session-Warning-Threshold", warningThreshold.ToString());
            
            var remainingTime = TimeSpan.FromMinutes(maxSessionTime) - (DateTime.UtcNow - sessionInfo.StartTime);
            context.Response.Headers.Add("X-Session-Remaining-Seconds", Math.Max(0, (int)remainingTime.TotalSeconds).ToString());
        }

        private async Task<UserSessionInfo> GetSessionInfoAsync(string sessionKey)
        {
            return await Task.FromResult(_cache.Get(sessionKey) as UserSessionInfo);
        }

        private async Task SetSessionInfoAsync(string sessionKey, UserSessionInfo sessionInfo)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Keep in cache for 24 hours
            };

            _cache.Set(sessionKey, sessionInfo, cacheOptions);
            await Task.CompletedTask;
        }

        private async Task SendSessionWarningAsync(HttpContext context, UserSessionInfo sessionInfo, TimeSpan timeUntilTimeout)
        {
            try
            {
                _logger.LogWarning("Session timeout warning sent to user {UserId}. Session expires in {Minutes} minutes",
                    sessionInfo.UserId, (int)timeUntilTimeout.TotalMinutes);

                // Add warning header for client-side notification
                context.Response.Headers.Add("X-Session-Warning", "timeout-imminent");
                context.Response.Headers.Add("X-Session-Warning-Minutes", ((int)timeUntilTimeout.TotalMinutes).ToString());

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send session warning to user {UserId}", sessionInfo.UserId);
            }
        }

        private async Task HandleSessionTimeoutAsync(HttpContext context, UserSessionInfo sessionInfo)
        {
            try
            {
                _logger.LogWarning("Session timed out for user {UserId}. Session duration: {Duration}",
                    sessionInfo.UserId, DateTime.UtcNow - sessionInfo.StartTime);

                // Clear session
                var sessionKey = $"user_session_{sessionInfo.UserId}";
                _cache.Remove(sessionKey);

                // Return 401 Unauthorized with session timeout indicator
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("X-Session-Expired", "true");
                context.Response.Headers.Add("X-Session-Timeout-Reason", "session-expired");

                await context.Response.WriteAsync("Session has expired. Please log in again.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle session timeout for user {UserId}", sessionInfo.UserId);
            }
        }

        private async Task HandleSuspiciousActivityAsync(HttpContext context, UserSessionInfo sessionInfo, string reason)
        {
            try
            {
                _logger.LogWarning("Suspicious activity detected for user {UserId}: {Reason}. Old IP: {OldIP}, New IP: {NewIP}",
                    sessionInfo.UserId, reason, sessionInfo.IpAddress, context.Connection.RemoteIpAddress?.ToString());

                // Log security event to activity log directly
                var activityLog = new ActivityLog
                {
                    UserId = sessionInfo.UserId,
                    Type = "SECURITY_SUSPICIOUS_ACTIVITY",
                    Description = reason,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Status = "failed",
                    Details = $"Session started from {sessionInfo.IpAddress}, now accessing from {context.Connection.RemoteIpAddress}"
                };

                // Use the context's database directly to avoid circular dependency
                var dbContext = context.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
                dbContext.ActivityLogs.Add(activityLog);
                await dbContext.SaveChangesAsync();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle suspicious activity for user {UserId}", sessionInfo.UserId);
            }
        }
    }

    public class UserSessionInfo
    {
        public string UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivity { get; set; }
        public bool WarningSent { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    /// <summary>
    /// Extension method for adding enhanced session middleware
    /// </summary>
    public static class EnhancedSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedSession(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnhancedSessionMiddleware>();
        }
    }
}
