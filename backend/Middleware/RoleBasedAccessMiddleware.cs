using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Middleware
{
    public class RoleBasedAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleBasedAccessMiddleware> _logger;
        private readonly IPermissionService _permissionService;

        public RoleBasedAccessMiddleware(
            RequestDelegate next,
            ILogger<RoleBasedAccessMiddleware> logger,
            IPermissionService permissionService)
        {
            _next = next;
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = context.User?.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    // Get user permissions and add to context for easy access
                    var permissions = await _permissionService.GetUserRolePermissionsAsync(userId, tenantId);
                    
                    // Add permissions to HttpContext items for controllers to use
                    context.Items["UserPermissions"] = permissions;
                    context.Items["UserId"] = userId;
                    context.Items["TenantId"] = tenantId;

                    // Log user access for audit purposes
                    _logger.LogDebug("User {UserId} with {PermissionCount} permissions accessing {Path}", 
                        userId, permissions.Count, context.Request.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading permissions for user {UserId}", userId);
                    // Continue without permissions - controllers should handle missing permissions
                }
            }

            await _next(context);
        }
    }
}
