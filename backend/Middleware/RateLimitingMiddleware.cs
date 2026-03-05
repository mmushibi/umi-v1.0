using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace UmiHealthPOS.Middleware
{
    /// <summary>
    /// API Rate Limiting Middleware with role-based limits
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IMemoryCache cache,
            RateLimitOptions options)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var identifier = GetClientIdentifier(context);
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Skip rate limiting for health checks and static files
            if (ShouldSkipRateLimiting(path))
            {
                await _next(context);
                return;
            }

            // Get rate limit based on user role
            var rateLimit = GetRateLimitForUser(context);
            var cacheKey = $"rate_limit_{identifier}_{path}";

            // Check current request count
            var requestCount = await GetRequestCountAsync(cacheKey);
            
            if (requestCount >= rateLimit.RequestsPerWindow)
            {
                _logger.LogWarning("Rate limit exceeded for {Identifier} on {Path}. Count: {Count}, Limit: {Limit}",
                    identifier, path, requestCount, rateLimit.RequestsPerWindow);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = rateLimit.WindowSeconds.ToString();
                
                await context.Response.WriteAsync($"Rate limit exceeded. Try again in {rateLimit.WindowSeconds} seconds.");
                return;
            }

            // Increment request count
            await IncrementRequestCountAsync(cacheKey, rateLimit.WindowSeconds);

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = rateLimit.RequestsPerWindow.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rateLimit.RequestsPerWindow - requestCount - 1).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = GetWindowExpiryTime(cacheKey).ToString();

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Try to get user ID from claims first
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user_{userId}";
            }

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                return $"ip_{ipAddress}";
            }

            return "unknown";
        }

        private bool ShouldSkipRateLimiting(string path)
        {
            return path.Contains("/health") ||
                   path.Contains("/ping") ||
                   path.StartsWith("/static") ||
                   path.StartsWith("/css") ||
                   path.StartsWith("/js") ||
                   path.StartsWith("/images") ||
                   path.EndsWith(".ico") ||
                   path.EndsWith(".css") ||
                   path.EndsWith(".js") ||
                   path.EndsWith(".png") ||
                   path.EndsWith(".jpg") ||
                   path.EndsWith(".gif");
        }

        private RateLimitRule GetRateLimitForUser(HttpContext context)
        {
            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            return userRole switch
            {
                "SuperAdmin" => _options.SuperAdmin,
                "Operations" => _options.Operations,
                "TenantAdmin" => _options.TenantAdmin,
                "Pharmacist" => _options.Pharmacist,
                "Cashier" => _options.Cashier,
                _ => _options.Default
            };
        }

        private async Task<int> GetRequestCountAsync(string cacheKey)
        {
            var cacheEntry = await Task.FromResult(_cache.Get(cacheKey) as CacheEntry);
            return cacheEntry?.Count ?? 0;
        }

        private async Task IncrementRequestCountAsync(string cacheKey, int windowSeconds)
        {
            var cacheEntry = await Task.FromResult(_cache.Get(cacheKey) as CacheEntry);
            
            if (cacheEntry == null)
            
            if (cacheEntry == null)
            {
                cacheEntry = new CacheEntry
                {
                    Count = 1,
                    Expiry = DateTime.UtcNow.AddSeconds(windowSeconds)
                };
            }
            else
            {
                cacheEntry.Count++;
            }

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = cacheEntry.Expiry
            };

            _cache.Set(cacheKey, cacheEntry, cacheOptions);
        }

        private DateTime GetWindowExpiryTime(string cacheKey)
        {
            var cacheEntry = _cache.Get(cacheKey) as CacheEntry;
            return cacheEntry?.Expiry ?? DateTime.UtcNow.AddSeconds(60);
        }
    }

    public class RateLimitOptions
    {
        public RateLimitRule SuperAdmin { get; set; } = new RateLimitRule { RequestsPerWindow = 1000, WindowSeconds = 60 };
        public RateLimitRule Operations { get; set; } = new RateLimitRule { RequestsPerWindow = 800, WindowSeconds = 60 };
        public RateLimitRule TenantAdmin { get; set; } = new RateLimitRule { RequestsPerWindow = 600, WindowSeconds = 60 };
        public RateLimitRule Pharmacist { get; set; } = new RateLimitRule { RequestsPerWindow = 400, WindowSeconds = 60 };
        public RateLimitRule Cashier { get; set; } = new RateLimitRule { RequestsPerWindow = 300, WindowSeconds = 60 };
        public RateLimitRule Default { get; set; } = new RateLimitRule { RequestsPerWindow = 100, WindowSeconds = 60 };
    }

    public class RateLimitRule
    {
        public int RequestsPerWindow { get; set; }
        public int WindowSeconds { get; set; }
    }

    public class CacheEntry
    {
        public int Count { get; set; }
        public DateTime Expiry { get; set; }
    }

    /// <summary>
    /// Extension method for adding rate limiting middleware
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitOptions options = null)
        {
            options = options ?? new RateLimitOptions();
            return builder.UseMiddleware<RateLimitingMiddleware>(options);
        }
    }
}
