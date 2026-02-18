using System.ComponentModel.DataAnnotations;
using UmiHealthPOS.Data;

namespace UmiHealthPOS.Models
{
    public enum UserRoleEnum
    {
        SuperAdmin = 1,    // Highest level - full system access
        Operations = 2,     // Second level - can manage users but not impersonate
        TenantAdmin = 3,   // Third level - tenant management
        Pharmacist = 4,    // Fourth level - pharmacy operations
        Cashier = 5        // Fifth level - sales operations
    }

    public static class UserRoleExtensions
    {
        public static string GetDisplayName(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => "Super Admin",
                UserRoleEnum.Operations => "Operations",
                UserRoleEnum.TenantAdmin => "Tenant Admin",
                UserRoleEnum.Pharmacist => "Pharmacist",
                UserRoleEnum.Cashier => "Cashier",
                _ => "Unknown"
            };
        }

        public static int GetHierarchyLevel(this UserRoleEnum role)
        {
            return (int)role;
        }

        public static bool CanAccessRole(this UserRoleEnum currentRole, UserRoleEnum targetRole)
        {
            return currentRole.GetHierarchyLevel() <= targetRole.GetHierarchyLevel();
        }

        public static bool CanImpersonateRole(this UserRoleEnum currentRole, UserRoleEnum targetRole)
        {
            // Only Super Admin can impersonate
            return currentRole == UserRoleEnum.SuperAdmin && currentRole != targetRole;
        }

        public static bool CanManageRole(this UserRoleEnum currentRole, UserRoleEnum targetRole)
        {
            // Super Admin can manage all roles except themselves
            // Operations can manage Tenant Admin, Pharmacist, Cashier
            // Tenant Admin can manage Pharmacist, Cashier
            return currentRole switch
            {
                UserRoleEnum.SuperAdmin => currentRole != targetRole,
                UserRoleEnum.Operations => currentRole.GetHierarchyLevel() <= targetRole.GetHierarchyLevel() && currentRole != targetRole,
                UserRoleEnum.TenantAdmin => currentRole.GetHierarchyLevel() <= targetRole.GetHierarchyLevel() && currentRole != targetRole,
                UserRoleEnum.Pharmacist => currentRole.GetHierarchyLevel() <= targetRole.GetHierarchyLevel() && currentRole != targetRole,
                UserRoleEnum.Cashier => false, // Cashier cannot manage any roles
                _ => false
            };
        }

        public static List<UserRoleEnum> GetManageableRoles(this UserRoleEnum role)
        {
            return Enum.GetValues<UserRoleEnum>()
                .Where(r => role.CanManageRole(r))
                .OrderBy(r => r.GetHierarchyLevel())
                .ToList();
        }
    }

    // Enhanced user session with impersonation support
    public class EnhancedUserSession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        public UserRoleEnum Role { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(100)]
        public string? DeviceInfo { get; set; }

        [StringLength(100)]
        public string? Browser { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Impersonation fields
        public bool IsImpersonated { get; set; } = false;

        [StringLength(450)]
        public string? ImpersonatedByUserId { get; set; }

        public UserRoleEnum? OriginalRole { get; set; }

        [StringLength(6)]
        public string? OriginalTenantId { get; set; }

        public DateTime? ImpersonatedAt { get; set; }

        // Navigation properties
        public virtual UserAccount User { get; set; } = null!;
        public virtual UserAccount? ImpersonatedByUser { get; set; }
    }

    // Impersonation audit log
    public class ImpersonationLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string SuperAdminUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string ImpersonatedUserId { get; set; } = string.Empty;

        public UserRoleEnum ImpersonatedRole { get; set; }

        [StringLength(6)]
        public string? TenantId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual UserAccount SuperAdminUser { get; set; } = null!;
        public virtual UserAccount ImpersonatedUser { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
    }

    // Row-level security context
    public class SecurityContext
    {
        public string UserId { get; set; } = string.Empty;
        public UserRoleEnum Role { get; set; }
        public string? TenantId { get; set; }
        public int? BranchId { get; set; }
        public bool IsImpersonated { get; set; }
        public string? ImpersonatedByUserId { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    // Permission definitions for fine-grained access control
    public static class SystemPermissions
    {
        // User Management
        public const string USER_CREATE = "user.create";
        public const string USER_READ = "user.read";
        public const string USER_UPDATE = "user.update";
        public const string USER_DELETE = "user.delete";
        public const string USER_RESET_PASSWORD = "user.reset_password";

        // Impersonation
        public const string IMPERSONATE_USER = "impersonate.user";
        public const string VIEW_IMPERSONATION_LOGS = "impersonate.view_logs";

        // Tenant Management
        public const string TENANT_CREATE = "tenant.create";
        public const string TENANT_READ = "tenant.read";
        public const string TENANT_UPDATE = "tenant.update";
        public const string TENANT_DELETE = "tenant.delete";

        // System Administration
        public const string SYSTEM_SETTINGS = "system.settings";
        public const string SYSTEM_AUDIT_LOGS = "system.audit_logs";
        public const string SYSTEM_BACKUP = "system.backup";
        public const string SYSTEM_MONITORING = "system.monitoring";

        // Inventory Management
        public const string INVENTORY_CREATE = "inventory.create";
        public const string INVENTORY_READ = "inventory.read";
        public const string INVENTORY_UPDATE = "inventory.update";
        public const string INVENTORY_DELETE = "inventory.delete";
        public const string INVENTORY_IMPORT = "inventory.import";
        public const string INVENTORY_EXPORT = "inventory.export";

        // Sales Management
        public const string SALES_CREATE = "sales.create";
        public const string SALES_READ = "sales.read";
        public const string SALES_UPDATE = "sales.update";
        public const string SALES_DELETE = "sales.delete";
        public const string SALES_REFUND = "sales.refund";

        // Clinical Operations
        public const string PRESCRIPTION_CREATE = "prescription.create";
        public const string PRESCRIPTION_READ = "prescription.read";
        public const string PRESCRIPTION_UPDATE = "prescription.update";
        public const string PRESCRIPTION_DELETE = "prescription.delete";
        public const string PATIENT_MANAGE = "patient.manage";

        // Reporting
        public const string REPORTS_VIEW = "reports.view";
        public const string REPORTS_CREATE = "reports.create";
        public const string REPORTS_EXPORT = "reports.export";
        public const string REPORTS_SCHEDULE = "reports.schedule";
    }

    // Role permission mapping
    public static class RolePermissions
    {
        public static Dictionary<UserRoleEnum, List<string>> DefaultPermissions = new()
        {
            [UserRoleEnum.SuperAdmin] = new()
            {
                // All permissions
                SystemPermissions.USER_CREATE, SystemPermissions.USER_READ, SystemPermissions.USER_UPDATE, SystemPermissions.USER_DELETE, SystemPermissions.USER_RESET_PASSWORD,
                SystemPermissions.IMPERSONATE_USER, SystemPermissions.VIEW_IMPERSONATION_LOGS,
                SystemPermissions.TENANT_CREATE, SystemPermissions.TENANT_READ, SystemPermissions.TENANT_UPDATE, SystemPermissions.TENANT_DELETE,
                SystemPermissions.SYSTEM_SETTINGS, SystemPermissions.SYSTEM_AUDIT_LOGS, SystemPermissions.SYSTEM_BACKUP, SystemPermissions.SYSTEM_MONITORING,
                SystemPermissions.INVENTORY_CREATE, SystemPermissions.INVENTORY_READ, SystemPermissions.INVENTORY_UPDATE, SystemPermissions.INVENTORY_DELETE, SystemPermissions.INVENTORY_IMPORT, SystemPermissions.INVENTORY_EXPORT,
                SystemPermissions.SALES_CREATE, SystemPermissions.SALES_READ, SystemPermissions.SALES_UPDATE, SystemPermissions.SALES_DELETE, SystemPermissions.SALES_REFUND,
                SystemPermissions.PRESCRIPTION_CREATE, SystemPermissions.PRESCRIPTION_READ, SystemPermissions.PRESCRIPTION_UPDATE, SystemPermissions.PRESCRIPTION_DELETE, SystemPermissions.PATIENT_MANAGE,
                SystemPermissions.REPORTS_VIEW, SystemPermissions.REPORTS_CREATE, SystemPermissions.REPORTS_EXPORT, SystemPermissions.REPORTS_SCHEDULE
            },
            [UserRoleEnum.Operations] = new()
            {
                // User management (except Super Admin)
                SystemPermissions.USER_CREATE, SystemPermissions.USER_READ, SystemPermissions.USER_UPDATE, SystemPermissions.USER_DELETE, SystemPermissions.USER_RESET_PASSWORD,
                // Tenant management
                SystemPermissions.TENANT_READ, SystemPermissions.TENANT_UPDATE,
                // System monitoring (no settings)
                SystemPermissions.SYSTEM_MONITORING,
                // Full inventory and sales
                SystemPermissions.INVENTORY_CREATE, SystemPermissions.INVENTORY_READ, SystemPermissions.INVENTORY_UPDATE, SystemPermissions.INVENTORY_DELETE, SystemPermissions.INVENTORY_IMPORT, SystemPermissions.INVENTORY_EXPORT,
                SystemPermissions.SALES_CREATE, SystemPermissions.SALES_READ, SystemPermissions.SALES_UPDATE, SystemPermissions.SALES_DELETE, SystemPermissions.SALES_REFUND,
                // Clinical operations
                SystemPermissions.PRESCRIPTION_CREATE, SystemPermissions.PRESCRIPTION_READ, SystemPermissions.PRESCRIPTION_UPDATE, SystemPermissions.PRESCRIPTION_DELETE, SystemPermissions.PATIENT_MANAGE,
                // Reporting
                SystemPermissions.REPORTS_VIEW, SystemPermissions.REPORTS_CREATE, SystemPermissions.REPORTS_EXPORT, SystemPermissions.REPORTS_SCHEDULE
            },
            [UserRoleEnum.TenantAdmin] = new()
            {
                // User management for tenant users only
                SystemPermissions.USER_CREATE, SystemPermissions.USER_READ, SystemPermissions.USER_UPDATE, SystemPermissions.USER_RESET_PASSWORD,
                // Full tenant operations
                SystemPermissions.INVENTORY_CREATE, SystemPermissions.INVENTORY_READ, SystemPermissions.INVENTORY_UPDATE, SystemPermissions.INVENTORY_DELETE, SystemPermissions.INVENTORY_IMPORT, SystemPermissions.INVENTORY_EXPORT,
                SystemPermissions.SALES_CREATE, SystemPermissions.SALES_READ, SystemPermissions.SALES_UPDATE, SystemPermissions.SALES_DELETE, SystemPermissions.SALES_REFUND,
                SystemPermissions.PRESCRIPTION_CREATE, SystemPermissions.PRESCRIPTION_READ, SystemPermissions.PRESCRIPTION_UPDATE, SystemPermissions.PRESCRIPTION_DELETE, SystemPermissions.PATIENT_MANAGE,
                // Reporting
                SystemPermissions.REPORTS_VIEW, SystemPermissions.REPORTS_CREATE, SystemPermissions.REPORTS_EXPORT, SystemPermissions.REPORTS_SCHEDULE
            },
            [UserRoleEnum.Pharmacist] = new()
            {
                // Read-only user management
                SystemPermissions.USER_READ,
                // Inventory with limited write access
                SystemPermissions.INVENTORY_READ, SystemPermissions.INVENTORY_CREATE, SystemPermissions.INVENTORY_UPDATE,
                SystemPermissions.INVENTORY_EXPORT,
                // Sales operations
                SystemPermissions.SALES_CREATE, SystemPermissions.SALES_READ,
                // Full clinical operations
                SystemPermissions.PRESCRIPTION_CREATE, SystemPermissions.PRESCRIPTION_READ, SystemPermissions.PRESCRIPTION_UPDATE, SystemPermissions.PATIENT_MANAGE,
                // Reporting
                SystemPermissions.REPORTS_VIEW, SystemPermissions.REPORTS_EXPORT
            },
            [UserRoleEnum.Cashier] = new()
            {
                // Read-only access
                SystemPermissions.USER_READ,
                SystemPermissions.INVENTORY_READ, SystemPermissions.INVENTORY_EXPORT,
                // Sales operations only
                SystemPermissions.SALES_CREATE, SystemPermissions.SALES_READ,
                // Limited reporting
                SystemPermissions.REPORTS_VIEW, SystemPermissions.REPORTS_EXPORT
            }
        };

        public static List<string> GetPermissionsForRole(UserRoleEnum role)
        {
            return DefaultPermissions.TryGetValue(role, out var permissions) ? permissions : new List<string>();
        }

        public static bool HasPermission(UserRoleEnum role, string permission)
        {
            var permissions = GetPermissionsForRole(role);
            return permissions.Contains(permission);
        }
    }
}
