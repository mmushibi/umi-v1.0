using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Middleware
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubscriptionMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Features that require subscription checks
        private readonly HashSet<string> _restrictedFeatures = new(StringComparer.OrdinalIgnoreCase)
        {
            "inventory", "products", "sales", "reports", "analytics", "users", "branches",
            "prescriptions", "patients", "suppliers", "compliance", "shifts", "billing"
        };

        public SubscriptionMiddleware(
            RequestDelegate next,
            ILogger<SubscriptionMiddleware> logger,
            IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip subscription checks for certain paths
            if (ShouldSkipSubscriptionCheck(context))
            {
                await _next(context);
                return;
            }

            try
            {
                var subscriptionCheck = await CheckSubscriptionAccess(context);
                
                if (!subscriptionCheck.IsAllowed)
                {
                    await HandleSubscriptionViolation(context, subscriptionCheck);
                    return;
                }

                // Add subscription info to response headers for frontend
                AddSubscriptionHeaders(context, subscriptionCheck.Subscription);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription middleware");
                await _next(context); // Continue on error
            }
        }

        private bool ShouldSkipSubscriptionCheck(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            
            // Skip authentication and subscription endpoints
            var skipPaths = new[]
            {
                "/api/auth", "/api/account/login", "/api/account/register",
                "/api/subscription", "/api/billing", "/health", "/swagger"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath)) ||
                   path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".png") ||
                   path.EndsWith(".jpg") || path.EndsWith(".ico") || path.EndsWith(".svg");
        }

        private async Task<SubscriptionCheckResult> CheckSubscriptionAccess(HttpContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var usageService = scope.ServiceProvider.GetRequiredService<IUsageTrackingService>();

            // Get user from token
            var user = await GetCurrentUserAsync(context, dbContext);
            if (user == null)
            {
                return new SubscriptionCheckResult
                {
                    IsAllowed = false,
                    Reason = "User not authenticated",
                    ErrorCode = "AUTH_REQUIRED"
                };
            }

            // Get subscription
            var subscription = await dbContext.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == user.TenantId && 
                                       (s.Status == "active" || s.Status == "grace_period"));

            if (subscription == null)
            {
                return new SubscriptionCheckResult
                {
                    IsAllowed = false,
                    Reason = "No active subscription found",
                    ErrorCode = "NO_SUBSCRIPTION",
                    User = user
                };
            }

            // Check if expired
            if (subscription.Status == "grace_period")
            {
                _logger.LogWarning("Tenant {TenantId} is in grace period", user.TenantId);
            }

            // Check feature access
            var requestedFeature = GetRequestedFeature(context);
            if (!string.IsNullOrEmpty(requestedFeature) && !HasFeatureAccess(subscription, requestedFeature))
            {
                return new SubscriptionCheckResult
                {
                    IsAllowed = false,
                    Reason = $"Feature '{requestedFeature}' not available in your subscription plan",
                    ErrorCode = "FEATURE_NOT_AVAILABLE",
                    RequiredPlan = GetRequiredPlanForFeature(requestedFeature),
                    Subscription = subscription,
                    User = user
                };
            }

            // Check usage limits
            var usageMetrics = await usageService.GetUsageMetricsAsync(user.TenantId);
            var limitViolation = await CheckUsageLimits(subscription, usageMetrics, requestedFeature);

            if (limitViolation != null)
            {
                return new SubscriptionCheckResult
                {
                    IsAllowed = false,
                    Reason = limitViolation.Reason,
                    ErrorCode = "LIMIT_EXCEEDED",
                    LimitType = limitViolation.Type,
                    CurrentUsage = limitViolation.Current,
                    Limit = limitViolation.Limit,
                    Subscription = subscription,
                    User = user
                };
            }

            return new SubscriptionCheckResult
            {
                IsAllowed = true,
                Subscription = subscription,
                User = user,
                UsageMetrics = usageMetrics
            };
        }

        private async Task<UserAccount?> GetCurrentUserAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            try
            {
                var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return null;

                var userId = userIdClaim.Value;
                return await dbContext.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
            }
            catch
            {
                return null;
            }
        }

        private string GetRequestedFeature(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            
            // Extract feature from URL path
            if (path.Contains("/api/"))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 2)
                {
                    return segments[2]; // /api/{feature}/{action}
                }
            }

            return string.Empty;
        }

        private bool HasFeatureAccess(Subscription subscription, string feature)
        {
            if (subscription.Plan?.Features == null) return false;

            try
            {
                var features = JsonSerializer.Deserialize<List<string>>(subscription.Plan.Features);
                if (features == null) return false;

                // Check if feature is in the plan's features
                var featureMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["inventory"] = "Inventory Management",
                    ["products"] = "Inventory Management",
                    ["sales"] = "Point of Sale",
                    ["reports"] = "Basic Reports",
                    ["analytics"] = "Advanced Analytics",
                    ["users"] = "User Management",
                    ["branches"] = "Multi-Branch Management",
                    ["prescriptions"] = "Prescription Management",
                    ["patients"] = "Patient Management",
                    ["suppliers"] = "Supplier Management",
                    ["compliance"] = "Compliance Reporting",
                    ["shifts"] = "Shift Management",
                    ["billing"] = "Billing Management"
                };

                var requiredFeature = featureMappings.GetValueOrDefault(feature, feature);
                return features.Contains(requiredFeature, StringComparer.OrdinalIgnoreCase) ||
                       features.Contains("All Features", StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string GetRequiredPlanForFeature(string feature)
        {
            return feature.ToLowerInvariant() switch
            {
                "analytics" => "Professional",
                "branches" => "Professional",
                "compliance" => "Professional",
                "users" => "Professional",
                var f when _restrictedFeatures.Contains(f) => "Professional",
                _ => "Starter"
            };
        }

        private async Task<LimitViolation?> CheckUsageLimits(Subscription subscription, UsageMetrics usageMetrics, string requestedFeature)
        {
            var plan = subscription.Plan;
            if (plan == null) return null;

            // Check specific limits based on requested feature
            switch (requestedFeature.ToLowerInvariant())
            {
                case "users":
                    if (usageMetrics.Users.Current >= plan.MaxUsers && plan.MaxUsers != -1)
                    {
                        return new LimitViolation
                        {
                            Type = "users",
                            Reason = $"User limit exceeded ({usageMetrics.Users.Current}/{plan.MaxUsers})",
                            Current = usageMetrics.Users.Current,
                            Limit = plan.MaxUsers
                        };
                    }
                    break;

                case "inventory":
                case "products":
                    if (usageMetrics.Products.Current >= plan.MaxProducts && plan.MaxProducts != -1)
                    {
                        return new LimitViolation
                        {
                            Type = "products",
                            Reason = $"Product limit exceeded ({usageMetrics.Products.Current}/{plan.MaxProducts})",
                            Current = usageMetrics.Products.Current,
                            Limit = plan.MaxProducts
                        };
                    }
                    break;

                case "sales":
                    if (usageMetrics.Transactions.Current >= plan.MaxTransactions && plan.MaxTransactions != -1)
                    {
                        return new LimitViolation
                        {
                            Type = "transactions",
                            Reason = $"Transaction limit exceeded ({usageMetrics.Transactions.Current}/{plan.MaxTransactions})",
                            Current = usageMetrics.Transactions.Current,
                            Limit = plan.MaxTransactions
                        };
                    }
                    break;

                case "branches":
                    if (usageMetrics.Branches.Current >= plan.MaxBranches && plan.MaxBranches != -1)
                    {
                        return new LimitViolation
                        {
                            Type = "branches",
                            Reason = $"Branch limit exceeded ({usageMetrics.Branches.Current}/{plan.MaxBranches})",
                            Current = usageMetrics.Branches.Current,
                            Limit = plan.MaxBranches
                        };
                    }
                    break;
            }

            return null;
        }

        private async Task HandleSubscriptionViolation(HttpContext context, SubscriptionCheckResult result)
        {
            context.Response.StatusCode = result.ErrorCode switch
            {
                "AUTH_REQUIRED" => 401,
                "NO_SUBSCRIPTION" => 402,
                "FEATURE_NOT_AVAILABLE" => 403,
                "LIMIT_EXCEEDED" => 429,
                _ => 403
            };

            context.Response.ContentType = "application/json";

            var response = new
            {
                error = true,
                errorCode = result.ErrorCode,
                message = result.Reason,
                requiredPlan = result.RequiredPlan,
                limitType = result.LimitType,
                currentUsage = result.CurrentUsage,
                limit = result.Limit,
                upgradeUrl = "/api/billing/upgrade",
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));

            // Log the violation
            _logger.LogWarning(
                "Subscription violation: {ErrorCode} - {Reason} for User {UserId}, Tenant {TenantId}",
                result.ErrorCode, result.Reason, result.User?.UserId, result.User?.TenantId);
        }

        private void AddSubscriptionHeaders(HttpContext context, Subscription? subscription)
        {
            if (subscription?.Plan != null)
            {
                context.Response.Headers["X-Subscription-Plan"] = subscription.Plan.Name;
                context.Response.Headers["X-Subscription-Status"] = subscription.Status;
                context.Response.Headers["X-Subscription-Expires"] = subscription.EndDate.ToString("yyyy-MM-dd");
            }
        }
    }

    // Supporting classes
    public class SubscriptionCheckResult
    {
        public bool IsAllowed { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string RequiredPlan { get; set; } = string.Empty;
        public string LimitType { get; set; } = string.Empty;
        public int? CurrentUsage { get; set; }
        public int? Limit { get; set; }
        public Subscription? Subscription { get; set; }
        public UserAccount? User { get; set; }
        public UsageMetrics? UsageMetrics { get; set; }
    }

    public class LimitViolation
    {
        public string Type { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int Current { get; set; }
        public int Limit { get; set; }
    }

    // Extension method for middleware registration
    public static class SubscriptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSubscriptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SubscriptionMiddleware>();
        }
    }
}
