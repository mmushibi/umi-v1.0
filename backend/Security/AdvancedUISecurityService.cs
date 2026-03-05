using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Security
{
    public interface IAdvancedUISecurityService
    {
        Task<bool> ValidateUIAccessAsync(string userId, string elementId, string action);
        Task<List<string>> GetAllowedEndpointsAsync(string userId);
        Task<bool> ValidateAntiForgeryTokenAsync(string userId, string token);
        Task LogUIAccessAsync(string userId, string elementId, string action, bool granted);
        Task<List<SecurityPolicy>> GetSecurityPoliciesAsync(string userId);
    }

    public class AdvancedUISecurityService : IAdvancedUISecurityService
    {
        private readonly ILogger<AdvancedUISecurityService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IRowLevelSecurityService _securityService;

        public AdvancedUISecurityService(
            ILogger<AdvancedUISecurityService> logger,
            IMemoryCache cache,
            IRowLevelSecurityService securityService)
        {
            _logger = logger;
            _cache = cache;
            _securityService = securityService;
        }

        public async Task<bool> ValidateUIAccessAsync(string userId, string elementId, string action)
        {
            try
            {
                // Multi-layer UI security validation
                var cacheKey = $"ui_access_{userId}_{elementId}_{action}";
                
                if (_cache.TryGetValue(cacheKey, out bool cachedResult))
                {
                    return cachedResult;
                }

                // Role-based validation
                var securityContext = await _securityService.GetSecurityContextAsync(
                    new System.Security.Claims.ClaimsPrincipal());
                
                var hasPermission = await ValidateElementPermissionAsync(securityContext.Role, elementId, action);
                
                // Time-based restrictions
                if (!await ValidateTimeBasedAccessAsync(securityContext.Role, elementId))
                {
                    await LogUIAccessAsync(userId, elementId, action, false);
                    return false;
                }

                // Location-based validation
                if (!await ValidateLocationBasedAccessAsync(userId, elementId))
                {
                    await LogUIAccessAsync(userId, elementId, action, false);
                    return false;
                }

                // Cache result for 5 minutes
                _cache.Set(cacheKey, hasPermission, TimeSpan.FromMinutes(5));

                await LogUIAccessAsync(userId, elementId, action, hasPermission);
                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating UI access for user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<string>> GetAllowedEndpointsAsync(string userId)
        {
            var securityContext = await _securityService.GetSecurityContextAsync(
                new System.Security.Claims.ClaimsPrincipal());

            var endpoints = GetRoleBasedEndpoints(securityContext.Role);
            
            return await Task.FromResult(endpoints);
        }

        public async Task<bool> ValidateAntiForgeryTokenAsync(string userId, string token)
        {
            var cacheKey = $"anti_forgery_{userId}";
            var storedToken = _cache.Get<string>(cacheKey);

            var isValid = !string.IsNullOrEmpty(storedToken) && storedToken == token;
            
            if (isValid)
            {
                // Invalidate token after use
                _cache.Remove(cacheKey);
            }

            return await Task.FromResult(isValid);
        }

        public async Task LogUIAccessAsync(string userId, string elementId, string action, bool granted)
        {
            _logger.LogInformation("UI Access - User: {UserId}, Element: {ElementId}, Action: {Action}, Granted: {Granted}",
                userId, elementId, action, granted);
            
            await Task.CompletedTask;
        }

        public async Task<List<SecurityPolicy>> GetSecurityPoliciesAsync(string userId)
        {
            var securityContext = await _securityService.GetSecurityContextAsync(
                new System.Security.Claims.ClaimsPrincipal());

            var policies = GetRoleBasedPolicies(securityContext.Role);
            
            return await Task.FromResult(policies);
        }

        private async Task<bool> ValidateElementPermissionAsync(UserRoleEnum role, string elementId, string action)
        {
            var permissions = GetRoleUIPermissions(role);
            var elementKey = $"{elementId}_{action}";
            
            return await Task.FromResult(permissions.Contains(elementKey));
        }

        private async Task<bool> ValidateTimeBasedAccessAsync(UserRoleEnum role, string elementId)
        {
            var now = DateTime.UtcNow.TimeOfDay;
            var restrictedElements = new[] { "admin_panel", "user_management", "system_settings" };
            
            if (!restrictedElements.Contains(elementId))
            {
                return await Task.FromResult(true);
            }

            var allowedStart = new TimeSpan(8, 0, 0); // 8 AM
            var allowedEnd = new TimeSpan(18, 0, 0);  // 6 PM

            var isWithinHours = now >= allowedStart && now <= allowedEnd;
            
            return await Task.FromResult(role >= UserRoleEnum.TenantAdmin || isWithinHours);
        }

        private async Task<bool> ValidateLocationBasedAccessAsync(string userId, string elementId)
        {
            // Implement location-based validation for sensitive operations
            var sensitiveElements = new[] { "financial_reports", "user_management", "system_backup" };
            
            if (!sensitiveElements.Contains(elementId))
            {
                return await Task.FromResult(true);
            }

            // Check if user is accessing from trusted location
            var cacheKey = $"trusted_location_{userId}";
            var isTrustedLocation = _cache.Get<bool>(cacheKey);
            
            return await Task.FromResult(isTrustedLocation);
        }

        private List<string> GetRoleBasedEndpoints(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => new List<string>
                {
                    "/api/superadmin/*",
                    "/api/compliance/*",
                    "/api/system/*",
                    "/api/tenants/*"
                },
                UserRoleEnum.Sales => new List<string>
                {
                    "/api/operations/*",
                    "/api/reports/*",
                    "/api/shifts/*"
                },
                UserRoleEnum.TenantAdmin => new List<string>
                {
                    "/api/tenantadmin/*",
                    "/api/inventory/*",
                    "/api/users/*",
                    "/api/reports/*"
                },
                UserRoleEnum.Pharmacist => new List<string>
                {
                    "/api/pharmacist/*",
                    "/api/prescriptions/*",
                    "/api/patients/*",
                    "/api/clinical/*"
                },
                UserRoleEnum.Cashier => new List<string>
                {
                    "/api/cashier/*",
                    "/api/sales/*",
                    "/api/customers/*"
                },
                _ => new List<string>()
            };
        }

        private List<string> GetRoleUIPermissions(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => new List<string>
                {
                    "dashboard_view", "users_manage", "tenants_manage", "system_settings",
                    "audit_logs", "compliance_reports", "security_monitoring"
                },
                UserRoleEnum.Sales => new List<string>
                {
                    "dashboard_view", "shifts_manage", "reports_view", "inventory_monitor",
                    "user_activity", "system_health"
                },
                UserRoleEnum.TenantAdmin => new List<string>
                {
                    "dashboard_view", "inventory_manage", "users_manage", "sales_reports",
                    "settings_manage", "branch_manage"
                },
                UserRoleEnum.Pharmacist => new List<string>
                {
                    "dashboard_view", "prescriptions_manage", "patients_view", "clinical_tools",
                    "inventory_view", "drug_interactions"
                },
                UserRoleEnum.Cashier => new List<string>
                {
                    "dashboard_view", "sales_process", "customers_view", "inventory_view",
                    "payment_process", "receipt_print"
                },
                _ => new List<string>()
            };
        }

        private List<SecurityPolicy> GetRoleBasedPolicies(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => new List<SecurityPolicy>
                {
                    new SecurityPolicy { Name = "Multi-Factor Required", Enforced = true },
                    new SecurityPolicy { Name = "Session Timeout", Enforced = true, Value = "30 minutes" },
                    new SecurityPolicy { Name = "Password Complexity", Enforced = true },
                    new SecurityPolicy { Name = "IP Restrictions", Enforced = false }
                },
                UserRoleEnum.TenantAdmin => new List<SecurityPolicy>
                {
                    new SecurityPolicy { Name = "Multi-Factor Required", Enforced = true },
                    new SecurityPolicy { Name = "Session Timeout", Enforced = true, Value = "60 minutes" },
                    new SecurityPolicy { Name = "Password Complexity", Enforced = true },
                    new SecurityPolicy { Name = "Business Hours Only", Enforced = true }
                },
                UserRoleEnum.Pharmacist => new List<SecurityPolicy>
                {
                    new SecurityPolicy { Name = "Session Timeout", Enforced = true, Value = "120 minutes" },
                    new SecurityPolicy { Name = "Prescription Limits", Enforced = true },
                    new SecurityPolicy { Name = "Drug Access Logging", Enforced = true }
                },
                UserRoleEnum.Cashier => new List<SecurityPolicy>
                {
                    new SecurityPolicy { Name = "Session Timeout", Enforced = true, Value = "90 minutes" },
                    new SecurityPolicy { Name = "Transaction Limits", Enforced = true },
                    new SecurityPolicy { Name = "Cash Drawer Limits", Enforced = true }
                },
                _ => new List<SecurityPolicy>()
            };
        }
    }

    public class SecurityPolicy
    {
        public string Name { get; set; }
        public bool Enforced { get; set; }
        public string Value { get; set; }
    }
}
