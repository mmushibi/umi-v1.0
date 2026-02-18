using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using UmiHealthPOS.Models;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Security
{
    public interface IRowLevelSecurityService
    {
        Task<SecurityContext> GetSecurityContextAsync(ClaimsPrincipal user);
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
        Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, string tenantId);
        Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, int? branchId);
        Task<IQueryable<T>> ApplyTenantFilter<T>(IQueryable<T> query, ClaimsPrincipal user) where T : class;
        Task<IQueryable<T>> ApplyBranchFilter<T>(IQueryable<T> query, ClaimsPrincipal user) where T : class;
    }

    public class RowLevelSecurityService : IRowLevelSecurityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RowLevelSecurityService> _logger;

        public RowLevelSecurityService(ApplicationDbContext context, ILogger<RowLevelSecurityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SecurityContext> GetSecurityContextAsync(ClaimsPrincipal user)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            var userAccount = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userAccount == null)
            {
                throw new UnauthorizedAccessException("User account not found");
            }

            // Check for active impersonation
            var activeSession = await _context.Set<EnhancedUserSession>()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            var securityContext = new SecurityContext
            {
                UserId = userId,
                Role = Enum.Parse<UserRoleEnum>(userAccount.Role),
                TenantId = userAccount.TenantId,
                BranchId = userAccount.BranchId,
                IsImpersonated = activeSession?.IsImpersonated ?? false,
                ImpersonatedByUserId = activeSession?.ImpersonatedByUserId,
                Permissions = RolePermissions.GetPermissionsForRole(Enum.Parse<UserRoleEnum>(userAccount.Role))
            };

            // If impersonated, get original context
            if (securityContext.IsImpersonated && !string.IsNullOrEmpty(activeSession?.ImpersonatedByUserId))
            {
                var impersonatedByUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == activeSession.ImpersonatedByUserId);

                if (impersonatedByUser != null)
                {
                    securityContext.ImpersonatedByUserId = activeSession.ImpersonatedByUserId;
                    // Add impersonation permissions
                    securityContext.Permissions.Add(SystemPermissions.IMPERSONATE_USER);
                }
            }

            return securityContext;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            try
            {
                var context = await GetSecurityContextAsync(user);
                return context.Permissions.Contains(permission) || context.Permissions.Contains(SystemPermissions.SYSTEM_SETTINGS);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user", permission);
                return false;
            }
        }

        public async Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, string tenantId)
        {
            try
            {
                var context = await GetSecurityContextAsync(user);

                // Super Admin can access all tenants
                if (context.Role == UserRoleEnum.SuperAdmin)
                    return true;

                // Operations can access all tenants for monitoring
                if (context.Role == UserRoleEnum.Operations)
                    return true;

                // Other roles can only access their own tenant
                return context.TenantId == tenantId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking tenant access for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, int? branchId)
        {
            try
            {
                var context = await GetSecurityContextAsync(user);

                // Super Admin and Operations can access all branches
                if (context.Role == UserRoleEnum.SuperAdmin || context.Role == UserRoleEnum.Operations)
                    return true;

                // Tenant Admin can access all branches in their tenant
                if (context.Role == UserRoleEnum.TenantAdmin)
                {
                    if (!branchId.HasValue) return true;
                    var branch = await _context.Branches.FindAsync(branchId.Value);
                    return branch?.TenantId == context.TenantId;
                }

                // Other roles can only access their assigned branch
                return context.BranchId == branchId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking branch access for branch {BranchId}", branchId);
                return false;
            }
        }

        public async Task<IQueryable<T>> ApplyTenantFilter<T>(IQueryable<T> query, ClaimsPrincipal user) where T : class
        {
            var context = await GetSecurityContextAsync(user);

            // Super Admin and Operations see all data
            if (context.Role == UserRoleEnum.SuperAdmin || context.Role == UserRoleEnum.Operations)
                return query;

            // Apply tenant filter for other roles
            if (!string.IsNullOrEmpty(context.TenantId))
            {
                var tenantProperty = typeof(T).GetProperty("TenantId");
                if (tenantProperty != null)
                {
                    query = query.Where(e => EF.Property<string>(e, "TenantId") == context.TenantId);
                }
            }

            return query;
        }

        public async Task<IQueryable<T>> ApplyBranchFilter<T>(IQueryable<T> query, ClaimsPrincipal user) where T : class
        {
            var context = await GetSecurityContextAsync(user);

            // Super Admin and Operations see all data
            if (context.Role == UserRoleEnum.SuperAdmin || context.Role == UserRoleEnum.Operations)
                return query;

            // Tenant Admin sees all branches in their tenant (handled by tenant filter)
            if (context.Role == UserRoleEnum.TenantAdmin)
                return query;

            // Apply branch filter for other roles
            if (context.BranchId.HasValue)
            {
                var branchProperty = typeof(T).GetProperty("BranchId");
                if (branchProperty != null)
                {
                    query = query.Where(e => EF.Property<int?>(e, "BranchId") == context.BranchId);
                }
            }

            return query;
        }
    }

    // Attribute for role-based authorization
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly IRowLevelSecurityService? _securityService;

        public RequirePermissionAttribute(params string[] permissions)
        {
            _permissions = permissions;
        }

        public RequirePermissionAttribute(IRowLevelSecurityService securityService, params string[] permissions)
        {
            _securityService = securityService;
            _permissions = permissions;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var securityService = context.HttpContext.RequestServices.GetRequiredService<IRowLevelSecurityService>();

            foreach (var permission in _permissions)
            {
                if (!await securityService.HasPermissionAsync(user, permission))
                {
                    _logger?.LogWarning("User {UserId} denied access to resource requiring permission {Permission}",
                        user.FindFirst(ClaimTypes.NameIdentifier)?.Value, permission);
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        private ILogger? _logger => _securityService?.GetType().Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ILogger<RowLevelSecurityService>")
            ?.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            ?.GetValue(null) as ILogger;
    }

    // Attribute for minimum role requirement
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireMinimumRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly UserRoleEnum _minimumRole;

        public RequireMinimumRoleAttribute(UserRoleEnum minimumRole)
        {
            _minimumRole = minimumRole;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var securityService = context.HttpContext.RequestServices.GetRequiredService<IRowLevelSecurityService>();
            var securityContext = await securityService.GetSecurityContextAsync(user);

            if (securityContext.Role.GetHierarchyLevel() > _minimumRole.GetHierarchyLevel())
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }

    // Middleware for automatic row-level security
    public class RowLevelSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RowLevelSecurityMiddleware> _logger;

        public RowLevelSecurityMiddleware(RequestDelegate next, ILogger<RowLevelSecurityMiddleware> logger)
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
                    var securityService = context.RequestServices.GetRequiredService<IRowLevelSecurityService>();
                    var securityContext = await securityService.GetSecurityContextAsync(context.User);

                    // Add security context to HttpContext items for easy access
                    context.Items["SecurityContext"] = securityContext;

                    _logger.LogDebug("Security context established for user {UserId} with role {Role}",
                        securityContext.UserId, securityContext.Role);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error establishing security context for user");
                    // Don't block the request, but log the error
                }
            }

            await _next(context);
        }
    }

    // Extension methods for easy access to security context
    public static class SecurityContextExtensions
    {
        public static SecurityContext? GetSecurityContext(this HttpContext context)
        {
            return context.Items.TryGetValue("SecurityContext", out var contextObj)
                ? contextObj as SecurityContext
                : null;
        }

        public static async Task<bool> HasPermissionAsync(this HttpContext context, string permission)
        {
            var securityService = context.RequestServices.GetRequiredService<IRowLevelSecurityService>();
            return await securityService.HasPermissionAsync(context.User, permission);
        }

        public static async Task<bool> CanAccessTenantAsync(this HttpContext context, string tenantId)
        {
            var securityService = context.RequestServices.GetRequiredService<IRowLevelSecurityService>();
            return await securityService.CanAccessTenantAsync(context.User, tenantId);
        }

        public static async Task<bool> CanAccessBranchAsync(this HttpContext context, int? branchId)
        {
            var securityService = context.RequestServices.GetRequiredService<IRowLevelSecurityService>();
            return await securityService.CanAccessBranchAsync(context.User, branchId);
        }
    }
}
