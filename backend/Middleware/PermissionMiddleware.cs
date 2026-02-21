using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealthPOS.Services;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Middleware
{
    /// <summary>
    /// Middleware to automatically add user permissions to the HttpContext
    /// </summary>
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var permissionService = context.RequestServices.GetService<IPermissionService>();
                    if (permissionService != null)
                    {
                        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                    context.User.FindFirst("sub")?.Value;

                        if (!string.IsNullOrEmpty(userId))
                        {
                            // Get user permissions and add them to the context items
                            var userPermissions = await permissionService.GetUserPermissionsAsync(userId);
                            context.Items["UserPermissions"] = userPermissions;

                            // Add individual permissions as claims for easier access
                            var rolePermissions = await permissionService.GetUserRolePermissionsAsync(userId);
                            var claimsIdentity = context.User.Identity as ClaimsIdentity;

                            if (claimsIdentity != null)
                            {
                                foreach (var permission in rolePermissions)
                                {
                                    // Avoid duplicate claims
                                    if (!claimsIdentity.HasClaim("permission", permission))
                                    {
                                        claimsIdentity.AddClaim(new Claim("permission", permission));
                                    }
                                }
                            }

                            _logger.LogDebug("Added {Count} permissions to user {UserId}", rolePermissions.Count, userId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding permissions to context for user");
                    // Continue without permissions rather than blocking the request
                }
            }

            await _next(context);
        }
    }
}
