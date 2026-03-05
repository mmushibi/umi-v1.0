using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UmiHealthPOS.Services;
using UmiHealthPOS.Attributes;
using UmiHealthPOS.Security;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers.Api
{
    /// <summary>
    /// Provides server-side role and permission validation for UI elements
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [UmiHealthPOS.Attributes.RequirePermission("VIEW_UI")]
    public class UIPermissionsController : BaseController
    {
        private readonly IPermissionService _permissionService;

        public UIPermissionsController(
            ApplicationDbContext context,
            ILogger<UIPermissionsController> logger,
            IRowLevelSecurityService securityService,
            IAuditService auditService,
            IPermissionService permissionService) : base(context, logger, securityService, auditService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// Gets current user's permissions and UI access rights
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<UIPermissionsResponse>> GetCurrentPermissions()
        {
            try
            {
                var securityContext = await GetSecurityContextAsync();
                if (securityContext?.UserId == null)
                {
                    return Unauthorized("User not authenticated");
                }
                
                var userId = securityContext.UserId;

                // Get user's permissions
                var permissions = await _permissionService.GetUserRolePermissionsAsync(userId) ?? [];

                // Build UI permissions based on role and permissions
                var uiPermissions = new UIPermissionsResponse
                {
                    UserId = userId,
                    Role = securityContext.Role.ToString(),
                    TenantId = securityContext.TenantId ?? string.Empty,
                    BranchId = securityContext.BranchId,
                    Permissions = permissions,
                    UIElements = GetUIPermissionsForRole(securityContext.Role, permissions),
                    Navigation = GetNavigationPermissionsForRole(securityContext.Role),
                    Actions = GetActionPermissionsForRole(securityContext.Role),
                    DashboardWidgets = GetDashboardWidgetsForRole(securityContext.Role),
                    IsImpersonated = securityContext.IsImpersonated,
                    ImpersonatedByUserId = securityContext.ImpersonatedByUserId
                };

                await LogAccessAsync("READ_SUCCESS", "UI_PERMISSIONS", userId);
                return Ok(uiPermissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current UI permissions");
                await LogAccessAsync("READ_ERROR", "UI_PERMISSIONS", null, ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Validates if user can access specific UI element
        /// </summary>
        [HttpPost("validate-element")]
        public async Task<ActionResult<ElementAccessResponse>> ValidateElementAccess([FromBody] ElementAccessRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.ElementId))
                {
                    return BadRequest("ElementId is required");
                }
                
                var securityContext = await GetSecurityContextAsync();
                if (securityContext == null)
                {
                    return Unauthorized("User not authenticated");
                }
                
                var hasAccess = await CanAccessUIElementAsync(request.ElementId, securityContext);

                await LogAccessAsync("VALIDATE_ELEMENT", "UI_ELEMENT", request.ElementId, hasAccess ? "Allowed" : "Denied");

                return Ok(new ElementAccessResponse
                {
                    ElementId = request.ElementId,
                    HasAccess = hasAccess,
                    Role = securityContext.Role.ToString(),
                    Reason = hasAccess ? "Access granted" : "Insufficient permissions"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating element access for {ElementId}", request.ElementId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Validates if user can perform specific action
        /// </summary>
        [HttpPost("validate-action")]
        public async Task<ActionResult<ActionAccessResponse>> ValidateActionAccess([FromBody] ActionAccessRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Permission))
                {
                    return BadRequest("Permission is required");
                }
                
                var securityContext = await GetSecurityContextAsync();
                if (securityContext == null)
                {
                    return Unauthorized("User not authenticated");
                }
                
                var hasPermission = await HasPermissionAsync(request.Permission);

                await LogAccessAsync("VALIDATE_ACTION", "UI_ACTION", request.Permission, hasPermission ? "Allowed" : "Denied");

                return Ok(new ActionAccessResponse
                {
                    Action = request.Action,
                    Permission = request.Permission,
                    HasAccess = hasPermission,
                    Role = securityContext.Role.ToString(),
                    Reason = hasPermission ? "Permission granted" : "Permission denied"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating action access for {Action}", request.Action);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets role-based navigation menu items
        /// </summary>
        [HttpGet("navigation")]
        public async Task<ActionResult<List<NavigationItem>>> GetNavigationMenu()
        {
            try
            {
                var securityContext = await GetSecurityContextAsync();
                if (securityContext == null)
                {
                    return Unauthorized("User not authenticated");
                }
                
                var navigation = GetNavigationPermissionsForRole(securityContext.Role);

                await LogAccessAsync("READ_SUCCESS", "NAVIGATION_MENU");
                return Ok(navigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting navigation menu");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<bool> CanAccessUIElementAsync(string elementId, Models.SecurityContext securityContext)
        {
            var elementPermissions = GetUIPermissionsForRole(securityContext.Role, securityContext.Permissions);
            
            await LogAccessAsync("CHECK_ELEMENT", "UI_ELEMENT", elementId, "Access checked");
            
            return elementPermissions.TryGetValue(elementId, out var hasAccess) && hasAccess;
        }

        private Dictionary<string, bool> GetUIPermissionsForRole(UserRoleEnum role, List<string> permissions)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => GetSuperAdminUIPermissions(),
                UserRoleEnum.Operations => GetOperationsUIPermissions(),
                UserRoleEnum.TenantAdmin => GetTenantAdminUIPermissions(),
                UserRoleEnum.Pharmacist => GetPharmacistUIPermissions(),
                UserRoleEnum.Cashier => GetCashierUIPermissions(),
                _ => new Dictionary<string, bool>()
            };
        }

        private static List<NavigationItem> GetNavigationPermissionsForRole(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => GetSuperAdminNavigation(),
                UserRoleEnum.Operations => GetOperationsNavigation(),
                UserRoleEnum.TenantAdmin => GetTenantAdminNavigation(),
                UserRoleEnum.Pharmacist => GetPharmacistNavigation(),
                UserRoleEnum.Cashier => GetCashierNavigation(),
                _ => []
            };
        }

        private static Dictionary<string, bool> GetActionPermissionsForRole(UserRoleEnum role)
        {
            var actions = new Dictionary<string, bool>();

            // Common actions
            actions["view_dashboard"] = true;
            actions["view_profile"] = true;
            actions["edit_profile"] = true;

            switch (role)
            {
                case UserRoleEnum.SuperAdmin:
                    actions["manage_tenants"] = true;
                    actions["manage_users"] = true;
                    actions["manage_system"] = true;
                    actions["view_reports"] = true;
                    actions["manage_compliance"] = true;
                    actions["impersonate_users"] = true;
                    break;

                case UserRoleEnum.Operations:
                    actions["monitor_tenants"] = true;
                    actions["view_reports"] = true;
                    actions["manage_compliance"] = true;
                    break;

                case UserRoleEnum.TenantAdmin:
                    actions["manage_inventory"] = true;
                    actions["manage_users"] = true;
                    actions["manage_branches"] = true;
                    actions["view_reports"] = true;
                    actions["manage_settings"] = true;
                    actions["import_export"] = true;
                    break;

                case UserRoleEnum.Pharmacist:
                    actions["manage_prescriptions"] = true;
                    actions["view_inventory"] = true;
                    actions["add_inventory"] = true;
                    actions["edit_inventory"] = true;
                    actions["manage_patients"] = true;
                    actions["view_clinical_tools"] = true;
                    break;

                case UserRoleEnum.Cashier:
                    actions["process_sales"] = true;
                    actions["view_inventory"] = true;
                    actions["manage_customers"] = true;
                    actions["view_reports"] = true;
                    break;
            }

            return actions;
        }

        private static List<string> GetDashboardWidgetsForRole(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => [
                    "system_overview", "tenant_stats", "user_activity", "revenue_chart", 
                    "system_health", "recent_activities", "top_performers"
                ],
                UserRoleEnum.Operations => [
                    "tenant_monitoring", "system_health", "compliance_status", "activity_log"
                ],
                UserRoleEnum.TenantAdmin => [
                    "revenue_summary", "sales_chart", "inventory_status", "user_activity",
                    "prescription_stats", "low_stock_alerts", "recent_transactions"
                ],
                UserRoleEnum.Pharmacist => [
                    "prescription_queue", "inventory_alerts", "patient_stats", "drug_interactions",
                    "clinical_reminders", "expiring_medications"
                ],
                UserRoleEnum.Cashier => [
                    "daily_sales", "quick_inventory", "customer_queue", "payment_methods",
                    "recent_transactions", "sales_targets"
                ],
                _ => []
            };
        }

        #region Role-specific UI Permissions

        private static Dictionary<string, bool> GetSuperAdminUIPermissions()
        {
            return new()
            {
                ["tenant_management"] = true,
                ["user_management"] = true,
                ["system_settings"] = true,
                ["compliance_monitoring"] = true,
                ["audit_logs"] = true,
                ["backup_management"] = true,
                ["feature_management"] = true,
                ["impersonation"] = true,
                ["all_tenants_data"] = true
            };
        }

        private static Dictionary<string, bool> GetOperationsUIPermissions()
        {
            return new()
            {
                ["tenant_monitoring"] = true,
                ["system_health"] = true,
                ["compliance_monitoring"] = true,
                ["audit_logs"] = true,
                ["reports"] = true,
                ["all_tenants_readonly"] = true
            };
        }

        private static Dictionary<string, bool> GetTenantAdminUIPermissions()
        {
            return new()
            {
                ["inventory_management"] = true,
                ["user_management"] = true,
                ["branch_management"] = true,
                ["reports"] = true,
                ["settings"] = true,
                ["import_export"] = true,
                ["prescription_management"] = true,
                ["patient_management"] = true
            };
        }

        private static Dictionary<string, bool> GetPharmacistUIPermissions()
        {
            return new()
            {
                ["prescription_management"] = true,
                ["inventory_view"] = true,
                ["inventory_add"] = true,
                ["inventory_edit"] = true,
                ["patient_management"] = true,
                ["clinical_tools"] = true,
                ["drug_interactions"] = true,
                ["reports_view"] = true
            };
        }

        private static Dictionary<string, bool> GetCashierUIPermissions()
        {
            return new()
            {
                ["sales_processing"] = true,
                ["inventory_view"] = true,
                ["customer_management"] = true,
                ["payment_processing"] = true,
                ["reports_view"] = true,
                ["daily_sales"] = true
            };
        }

        #endregion

        #region Navigation Items

        private static List<NavigationItem> GetSuperAdminNavigation()
        {
            return [
                new() { Id = "dashboard", Title = "System Dashboard", Icon = "dashboard", Path = "../Super-Admin/home.html" },
                new() { Id = "tenants", Title = "Tenant Management", Icon = "business", Path = "../Super-Admin/tenants.html" },
                new() { Id = "users", Title = "User Management", Icon = "people", Path = "../Super-Admin/user-management.html" },
                new() { Id = "compliance", Title = "Compliance", Icon = "gavel", Path = "../Super-Admin/compliance.html" },
                new() { Id = "audit", Title = "Audit Logs", Icon = "history", Path = "../Super-Admin/audit-logs.html" },
                new() { Id = "billing", Title = "Billing", Icon = "payments", Path = "../Super-Admin/billing.html" },
                new() { Id = "settings", Title = "System Settings", Icon = "settings", Path = "../Super-Admin/settings.html" }
            ];
        }

        private static List<NavigationItem> GetOperationsNavigation()
        {
            return [
                new() { Id = "dashboard", Title = "Operations Dashboard", Icon = "dashboard", Path = "../Sales-Operations/home.html" },
                new() { Id = "monitoring", Title = "System Monitoring", Icon = "monitor", Path = "../Sales-Operations/monitoring.html" },
                new() { Id = "compliance", Title = "Compliance Monitor", Icon = "gavel", Path = "../Sales-Operations/compliance.html" },
                new() { Id = "reports", Title = "Reports", Icon = "analytics", Path = "../Sales-Operations/reports.html" }
            ];
        }

        private static List<NavigationItem> GetTenantAdminNavigation()
        {
            return [
                new() { Id = "dashboard", Title = "Dashboard", Icon = "dashboard", Path = "../Tenant-Admin/home.html" },
                new() { Id = "inventory", Title = "Inventory", Icon = "inventory", Path = "../Tenant-Admin/inventory.html" },
                new() { Id = "users", Title = "User Management", Icon = "people", Path = "../Tenant-Admin/user-management.html" },
                new() { Id = "branches", Title = "Branches", Icon = "store", Path = "../Tenant-Admin/branches.html" },
                new() { Id = "reports", Title = "Reports", Icon = "analytics", Path = "../Tenant-Admin/reports.html" },
                new() { Id = "settings", Title = "Settings", Icon = "settings", Path = "../Tenant-Admin/settings.html" }
            ];
        }

        private static List<NavigationItem> GetPharmacistNavigation()
        {
            return [
                new() { Id = "dashboard", Title = "Pharmacist Dashboard", Icon = "dashboard", Path = "../Pharmacist/home.html" },
                new() { Id = "prescriptions", Title = "Prescriptions", Icon = "medication", Path = "../Pharmacist/prescriptions.html" },
                new() { Id = "inventory", Title = "Inventory", Icon = "inventory", Path = "../Pharmacist/inventory.html" },
                new() { Id = "patients", Title = "Patients", Icon = "people", Path = "../Pharmacist/patients.html" },
                new() { Id = "clinical", Title = "Clinical Tools", Icon = "medical", Path = "../Pharmacist/clinical.html" },
                new() { Id = "reports", Title = "Reports", Icon = "analytics", Path = "../Pharmacist/reports.html" }
            ];
        }

        private static List<NavigationItem> GetCashierNavigation()
        {
            return [
                new() { Id = "dashboard", Title = "Cashier Dashboard", Icon = "dashboard", Path = "../Cashier/home.html" },
                new() { Id = "sales", Title = "Point of Sale", Icon = "point_of_sale", Path = "../Cashier/pos.html" },
                new() { Id = "inventory", Title = "Inventory", Icon = "inventory", Path = "../Cashier/inventory.html" },
                new() { Id = "customers", Title = "Customers", Icon = "people", Path = "../Cashier/customers.html" },
                new() { Id = "reports", Title = "Sales Reports", Icon = "analytics", Path = "../Cashier/reports.html" }
            ];
        }

        #endregion
    }

    #region DTOs

    public class UIPermissionsResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public List<string> Permissions { get; set; } = [];
        public Dictionary<string, bool> UIElements { get; set; } = [];
        public List<NavigationItem> Navigation { get; set; } = [];
        public Dictionary<string, bool> Actions { get; set; } = [];
        public List<string> DashboardWidgets { get; set; } = [];
        public bool IsImpersonated { get; set; } = false;
        public string? ImpersonatedByUserId { get; set; }
    }

    public class ElementAccessRequest
    {
        public string ElementId { get; set; } = string.Empty;
    }

    public class ElementAccessResponse
    {
        public string ElementId { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ActionAccessRequest
    {
        public string Action { get; set; } = string.Empty;
        public string Permission { get; set; } = string.Empty;
    }

    public class ActionAccessResponse
    {
        public string Action { get; set; } = string.Empty;
        public string Permission { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class NavigationItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public List<NavigationItem> Children { get; set; } = [];
    }

    #endregion
}
