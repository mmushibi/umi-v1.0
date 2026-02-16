using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Middleware
{
    public class InactivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InactivityMiddleware> _logger;
        private readonly ISessionTimeoutService _sessionTimeoutService;

        public InactivityMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ILogger<InactivityMiddleware> logger, ISessionTimeoutService sessionTimeoutService)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _sessionTimeoutService = sessionTimeoutService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip inactivity check for login endpoints and static files
            if (context.Request.Path.StartsWithSegments("/api/auth") ||
                context.Request.Path.StartsWithSegments("/login") ||
                context.Request.Path.StartsWithSegments("/signin") ||
                context.Request.Path.StartsWithSegments("/signup") ||
                context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js") ||
                context.Request.Path.StartsWithSegments("/images"))
            {
                await _next(context);
                return;
            }

            // Check for valid JWT token
            var token = GetTokenFromRequest(context);
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            try
            {
                // Validate token and check inactivity
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = System.Text.Encoding.UTF8.GetBytes(_serviceProvider.GetRequiredService<IConfiguration>()["Jwt:Key"]);
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _serviceProvider.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _serviceProvider.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                if (principal != null)
                {
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var tenantId = principal.FindFirst("TenantId")?.Value;

                    if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tenantId))
                    {
                        // Check if session has expired using user-specific timeout
                        var isExpired = await _sessionTimeoutService.IsSessionExpiredAsync(userId, tenantId, token);

                        if (isExpired)
                        {
                            _logger.LogInformation($"User {userId} session expired due to inactivity");

                            // Return 401 Unauthorized to trigger logout
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var response = new
                            {
                                message = "Session expired due to inactivity. Please log in again.",
                                code = "INACTIVITY_LOGOUT"
                            };
                            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                            return;
                        }

                        // Update last access time for active sessions
                        await _sessionTimeoutService.UpdateUserLastActivityAsync(userId, tenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user inactivity");
            }

            await _next(context);
        }

        private string GetTokenFromRequest(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
            {
                return token.Substring("Bearer ".Length);
            }
            return token;
        }
    }
}
