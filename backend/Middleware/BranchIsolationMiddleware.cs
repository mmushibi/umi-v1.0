using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Middleware
{
    public class BranchIsolationMiddleware
    {
        private readonly RequestDelegate _next;

        public BranchIsolationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip branch isolation for authentication endpoints and static files
            if (context.Request.Path.StartsWithSegments("/api/auth") ||
                context.Request.Path.StartsWithSegments("/connect") ||
                context.Request.Path.StartsWithSegments("/.well-known") ||
                context.Request.Path.StartsWithSegments("/swagger") ||
                !context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Tenant admins bypass branch isolation
            if (userRole == "TenantAdmin" || string.IsNullOrEmpty(userIdClaim))
            {
                await _next(context);
                return;
            }

            // For non-admin users, set branch context
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                using var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get user's assigned branches
                var userBranches = await dbContext.UserBranches
                    .Where(ub => ub.UserId == userIdClaim && ub.IsActive)
                    .ToListAsync();

                if (userBranches.Any())
                {
                    // Add branch information to the HttpContext for later use
                    context.Items["UserBranchIds"] = userBranches.Select(ub => ub.BranchId).ToList();
                    context.Items["UserPermissions"] = userBranches.ToDictionary(ub => ub.BranchId, ub => ub.Permission);
                }
                else
                {
                    // User has no branch assignments - deny access
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access denied: No branch assignments found");
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class BranchIsolationExtensions
    {
        public static IApplicationBuilder UseBranchIsolation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BranchIsolationMiddleware>();
        }

        public static IQueryable<T> ApplyBranchFilter<T>(this IQueryable<T> query, HttpContext context) where T : class
        {
            var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // Tenant admins see all data
            if (userRole == "TenantAdmin")
                return query;

            // Get user's branch IDs from middleware
            if (context.Items.TryGetValue("UserBranchIds", out var branchIdsObj) &&
                branchIdsObj is System.Collections.Generic.List<int> branchIds)
            {
                // Apply branch filter based on entity type
                var entityType = typeof(T);
                var branchIdProperty = entityType.GetProperty("BranchId");

                if (branchIdProperty != null)
                {
                    // Build expression to filter by BranchId
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "BranchId");
                    var containsMethod = typeof(System.Collections.Generic.List<int>).GetMethod("Contains", new[] { typeof(int) });
                    var containsCall = System.Linq.Expressions.Expression.Call(
                        System.Linq.Expressions.Expression.Constant(branchIds),
                        containsMethod,
                        property);
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(containsCall, parameter);

                    return query.Where(lambda);
                }
            }

            // If no branch context, return empty query to prevent data leakage
            return query.Take(0);
        }

        public static bool HasBranchPermission(this HttpContext context, int branchId, string requiredPermission)
        {
            var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // Tenant admins have all permissions
            if (userRole == "TenantAdmin")
                return true;

            if (context.Items.TryGetValue("UserPermissions", out var permissionsObj) &&
                permissionsObj is System.Collections.Generic.Dictionary<int, string> permissions)
            {
                if (permissions.TryGetValue(branchId, out var userPermission))
                {
                    return requiredPermission switch
                    {
                        "read" => true, // All assigned users can read
                        "write" => userPermission == "write" || userPermission == "admin",
                        "admin" => userPermission == "admin",
                        _ => false
                    };
                }
            }

            return false;
        }

        public static System.Collections.Generic.List<int> GetUserBranchIds(this HttpContext context)
        {
            if (context.Items.TryGetValue("UserBranchIds", out var branchIdsObj) &&
                branchIdsObj is System.Collections.Generic.List<int> branchIds)
            {
                return branchIds;
            }

            return new System.Collections.Generic.List<int>();
        }
    }
}
