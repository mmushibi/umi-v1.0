using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UmiHealthPOS.Services;
using UmiHealthPOS.Models;
using System.Security.Claims;

namespace UmiHealthPOS.Attributes
{
    /// <summary>
    /// Attribute to require specific permissions for accessing a controller or action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        public string[] Permissions { get; }
        public string? TenantId { get; set; }

        public RequirePermissionAttribute(params string[] permissions)
        {
            Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skip authorization if AllowAnonymous attribute is present
            if (context.ActionDescriptor.EndpointMetadata
                .Any(em => em is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
            {
                return;
            }

            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices
                .GetService<IPermissionService>();

            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check each required permission
            foreach (var permission in Permissions)
            {
                var hasPermission = permissionService.HasPermissionAsync(userId, permission, TenantId).Result;
                if (!hasPermission)
                {
                    context.Result = new ForbidResult($"Access denied. Missing permission: {permission}");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Attribute to require specific roles for accessing a controller or action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        public string[] Roles { get; }

        public RequireRoleAttribute(params string[] roles)
        {
            Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skip authorization if AllowAnonymous attribute is present
            if (context.ActionDescriptor.EndpointMetadata
                .Any(em => em is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
            {
                return;
            }

            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices
                .GetService<IPermissionService>();

            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if user is in any of the required roles
            var hasRole = false;
            foreach (var role in Roles)
            {
                if (permissionService.IsInRoleAsync(userId, role).Result)
                {
                    hasRole = true;
                    break;
                }
            }

            if (!hasRole)
            {
                context.Result = new ForbidResult($"Access denied. Missing required role. Required roles: {string.Join(", ", Roles)}");
                return;
            }
        }
    }

    /// <summary>
    /// Combined attribute for requiring both permissions and roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequirePermissionAndRoleAttribute : Attribute, IAuthorizationFilter
    {
        public string[] Permissions { get; }
        public string[] Roles { get; }
        public string? TenantId { get; set; }

        public RequirePermissionAndRoleAttribute(string[] permissions, string[] roles)
        {
            Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skip authorization if AllowAnonymous attribute is present
            if (context.ActionDescriptor.EndpointMetadata
                .Any(em => em is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
            {
                return;
            }

            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices
                .GetService<IPermissionService>();

            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check permissions
            foreach (var permission in Permissions)
            {
                var hasPermission = permissionService.HasPermissionAsync(userId, permission, TenantId).Result;
                if (!hasPermission)
                {
                    context.Result = new ForbidResult($"Access denied. Missing permission: {permission}");
                    return;
                }
            }

            // Check roles
            var hasRequiredRole = false;
            foreach (var role in Roles)
            {
                if (permissionService.IsInRoleAsync(userId, role).Result)
                {
                    hasRequiredRole = true;
                    break;
                }
            }

            if (!hasRequiredRole)
            {
                context.Result = new ForbidResult($"Access denied. Missing required role. Required roles: {string.Join(", ", Roles)}");
                return;
            }
        }
    }
}
