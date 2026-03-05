using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UmiHealthPOS.Security
{
    public class AdvancedSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdvancedSecurityMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, int> _failedAttempts = new();

        public AdvancedSecurityMiddleware(RequestDelegate next, ILogger<AdvancedSecurityMiddleware> logger, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Enhanced security checks
            if (!await ValidateRequestAsync(context, clientIp, userAgent))
            {
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Security validation failed");
                return;
            }

            await _next(context);
        }

        private async Task<bool> ValidateRequestAsync(HttpContext context, string clientIp, string userAgent)
        {
            // Check for brute force attempts
            if (await IsBruteForceAttackAsync(clientIp))
            {
                _logger.LogWarning("Brute force attack detected from IP: {IP}", clientIp);
                return false;
            }

            // Check for suspicious user agents
            if (IsSuspiciousUserAgent(userAgent))
            {
                _logger.LogWarning("Suspicious user agent detected: {UserAgent}", userAgent);
                return false;
            }

            // Validate request size
            if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB limit
            {
                _logger.LogWarning("Request size too large: {Size} bytes", context.Request.ContentLength);
                return false;
            }

            return true;
        }

        private async Task<bool> IsBruteForceAttackAsync(string clientIp)
        {
            var cacheKey = $"failed_attempts_{clientIp}";
            var attempts = _cache.Get<int>(cacheKey);
            
            if (attempts >= 5)
            {
                return true;
            }

            return false;
        }

        private bool IsSuspiciousUserAgent(string userAgent)
        {
            var suspiciousPatterns = new[]
            {
                "sqlmap", "nikto", "dirb", "nmap", "masscan",
                "zap", "burp", "metasploit", "hydra", "john"
            };

            return suspiciousPatterns.Any(pattern => 
                userAgent.ToLower().Contains(pattern.ToLower()));
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public static class AdvancedSecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseAdvancedSecurity(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AdvancedSecurityMiddleware>();
        }
    }
}
