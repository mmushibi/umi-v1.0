using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Middleware
{
    public class InactivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InactivityMiddleware> _logger;

        public InactivityMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ILogger<InactivityMiddleware> logger)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip inactivity check for login endpoints and static files
            if (context.Request.Path.StartsWith("/api/auth") || 
                context.Request.Path.StartsWith("/login") ||
                context.Request.Path.StartsWith("/signin") ||
                context.Request.Path.StartsWith("/signup") ||
                context.Request.Path.StartsWith("/css") ||
                context.Request.Path.StartsWith("/js") ||
                context.Request.Path.StartsWith("/images"))
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
                    ValidateIssuer = _serviceProvider.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                    ValidateAudience = _serviceProvider.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters);
                if (principal != null)
                {
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        
                        // Check user session for inactivity
                        var session = await dbContext.UserSessions
                            .FirstOrDefaultAsync(s => s.UserId == userId && s.Token == token && s.IsActive);
                        
                        if (session != null)
                        {
                            // Check if session has expired due to inactivity (30 minutes)
                            if (DateTime.UtcNow > session.ExpiresAt)
                            {
                                _logger.LogInformation($"User {userId} session expired due to inactivity");
                                
                                // Deactivate the session
                                session.IsActive = false;
                                await dbContext.SaveChangesAsync();
                                
                                // Return 401 Unauthorized to trigger logout
                                context.Response.StatusCode = 401;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsAsync(new { 
                                    message = "Session expired due to inactivity. Please log in again.",
                                    code = "INACTIVITY_LOGOUT"
                                });
                                return;
                            }
                            
                            // Update last access time for active sessions
                            session.LastAccessAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                        }
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
