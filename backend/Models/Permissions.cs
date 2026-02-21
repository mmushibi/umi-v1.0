using System;
using System.Collections.Generic;

namespace UmiHealthPOS.Models
{
    // Permission categories for organization
    public enum PermissionCategory
    {
        General,
        Inventory,
        Sales,
        Patients,
        Prescriptions,
        Clinical,
        Reports,
        Users,
        Settings,
        System
    }

    // Individual permissions with hierarchical structure
    public enum SystemPermission
    {
        // General permissions
        ViewDashboard,
        AccessSystem,

        // Inventory permissions
        InventoryView,
        InventoryCreate,
        InventoryEdit,
        InventoryDelete,
        InventoryImport,
        InventoryExport,
        StockAdjust,
        LowStockAlert,

        // Sales permissions
        SalesView,
        SalesCreate,
        SalesEdit,
        SalesDelete,
        SalesRefund,
        ReceiptPrint,
        DaybookAccess,

        // Patient permissions
        PatientView,
        PatientCreate,
        PatientEdit,
        PatientDelete,
        PatientSearch,

        // Prescription permissions
        PrescriptionView,
        PrescriptionCreate,
        PrescriptionEdit,
        PrescriptionDelete,
        PrescriptionFill,
        PrescriptionDispense,

        // Clinical permissions
        ClinicalTools,
        DrugInteractions,
        AllergyCheck,
        DosageCalculator,
        ClinicalGuidelines,

        // Reports permissions
        ReportsView,
        ReportsCreate,
        ReportsExport,
        ReportsSchedule,
        AnalyticsAccess,

        // User management permissions
        UserView,
        UserCreate,
        UserEdit,
        UserDelete,
        UserRoles,
        UserPermissions,

        // Settings permissions
        SettingsView,
        SettingsEdit,
        SystemConfig,
        BranchManage,
        SupplierManage,
        CategoryManage,

        // System permissions
        SystemAdmin,
        TenantAdmin,
        Impersonate,
        AuditLog,
        Backup,
        Restore
    }

    // Role definitions with specific permission sets
    public enum SystemRole
    {
        SuperAdmin,
        TenantAdmin,
        Pharmacist,
        Cashier,
        Operations,
        Reports
    }

    // Permission service result
    public class PermissionCheckResult
    {
        public bool IsAllowed { get; set; }
        public required string Permission { get; set; }
        public required string UserId { get; set; }
        public required string Role { get; set; }
        public string? Reason { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    // User permissions summary
    public class UserPermissionsSummary
    {
        public required string UserId { get; set; }
        public required string Role { get; set; }
        public required List<Permission> Permissions { get; set; }
        public required List<Role> Roles { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // Permission constants for easy reference
    public static class PermissionConstants
    {
        // Inventory permissions
        public const string INVENTORY_VIEW = "InventoryView";
        public const string INVENTORY_CREATE = "InventoryCreate";
        public const string INVENTORY_EDIT = "InventoryEdit";
        public const string INVENTORY_DELETE = "InventoryDelete";
        public const string INVENTORY_IMPORT = "InventoryImport";
        public const string INVENTORY_EXPORT = "InventoryExport";

        // Sales permissions
        public const string SALES_VIEW = "SalesView";
        public const string SALES_CREATE = "SalesCreate";
        public const string SALES_EDIT = "SalesEdit";
        public const string SALES_DELETE = "SalesDelete";
        public const string SALES_REFUND = "SalesRefund";

        // Patient permissions
        public const string PATIENT_VIEW = "PatientView";
        public const string PATIENT_CREATE = "PatientCreate";
        public const string PATIENT_EDIT = "PatientEdit";
        public const string PATIENT_DELETE = "PatientDelete";

        // Prescription permissions
        public const string PRESCRIPTION_VIEW = "PrescriptionView";
        public const string PRESCRIPTION_CREATE = "PrescriptionCreate";
        public const string PRESCRIPTION_EDIT = "PrescriptionEdit";
        public const string PRESCRIPTION_DELETE = "PrescriptionDelete";
        public const string PRESCRIPTION_FILL = "PrescriptionFill";

        // Clinical permissions
        public const string CLINICAL_TOOLS = "ClinicalTools";
        public const string DRUG_INTERACTIONS = "DrugInteractions";
        public const string ALLERGY_CHECK = "AllergyCheck";
        public const string DOSAGE_CALCULATOR = "DosageCalculator";

        // User management permissions
        public const string USER_VIEW = "UserView";
        public const string USER_CREATE = "UserCreate";
        public const string USER_EDIT = "UserEdit";
        public const string USER_DELETE = "UserDelete";

        // System permissions
        public const string SYSTEM_ADMIN = "SystemAdmin";
        public const string TENANT_ADMIN = "TenantAdmin";
        public const string IMPERSONATE = "Impersonate";
    }

    // Role definitions with permission mappings
    public static class SystemRolePermissions
    {
        // Super Admin - All permissions
        public static readonly List<string> SuperAdminPermissions = new()
        {
            // All permissions for super admin
            PermissionConstants.SYSTEM_ADMIN, PermissionConstants.TENANT_ADMIN, PermissionConstants.IMPERSONATE,
            "AuditLog", "Backup", "Restore",
            PermissionConstants.USER_VIEW, PermissionConstants.USER_CREATE, PermissionConstants.USER_EDIT, PermissionConstants.USER_DELETE,
            "UserRoles", "UserPermissions",
            "SettingsView", "SettingsEdit", "SystemConfig",
            "BranchManage", "SupplierManage", "CategoryManage",
            PermissionConstants.INVENTORY_VIEW, PermissionConstants.INVENTORY_CREATE, PermissionConstants.INVENTORY_EDIT,
            PermissionConstants.INVENTORY_DELETE, PermissionConstants.INVENTORY_IMPORT, PermissionConstants.INVENTORY_EXPORT,
            "StockAdjust", "LowStockAlert",
            PermissionConstants.SALES_VIEW, PermissionConstants.SALES_CREATE, PermissionConstants.SALES_EDIT,
            PermissionConstants.SALES_DELETE, PermissionConstants.SALES_REFUND, "ReceiptPrint",
            "DaybookAccess", PermissionConstants.PATIENT_VIEW, PermissionConstants.PATIENT_CREATE,
            PermissionConstants.PATIENT_EDIT, PermissionConstants.PATIENT_DELETE, "PatientSearch",
            PermissionConstants.PRESCRIPTION_VIEW, PermissionConstants.PRESCRIPTION_CREATE, PermissionConstants.PRESCRIPTION_EDIT,
            PermissionConstants.PRESCRIPTION_DELETE, PermissionConstants.PRESCRIPTION_FILL, "PrescriptionDispense",
            PermissionConstants.CLINICAL_TOOLS, PermissionConstants.DRUG_INTERACTIONS, PermissionConstants.ALLERGY_CHECK,
            "DosageCalculator", "ClinicalGuidelines",
            "ReportsView", "ReportsCreate", "ReportsExport",
            "ReportsSchedule", "AnalyticsAccess",
            "ViewDashboard", "AccessSystem"
        };

        // Tenant Admin - Most permissions except system-level
        public static readonly List<string> TenantAdminPermissions = new()
        {
            PermissionConstants.TENANT_ADMIN, PermissionConstants.USER_VIEW, PermissionConstants.USER_CREATE,
            PermissionConstants.USER_EDIT, "UserRoles",
            "SettingsView", "SettingsEdit",
            "BranchManage", "SupplierManage", "CategoryManage",
            PermissionConstants.INVENTORY_VIEW, PermissionConstants.INVENTORY_CREATE, PermissionConstants.INVENTORY_EDIT,
            PermissionConstants.INVENTORY_DELETE, PermissionConstants.INVENTORY_IMPORT, PermissionConstants.INVENTORY_EXPORT,
            "StockAdjust", "LowStockAlert",
            PermissionConstants.SALES_VIEW, PermissionConstants.SALES_CREATE, PermissionConstants.SALES_EDIT,
            PermissionConstants.SALES_REFUND, "ReceiptPrint", "DaybookAccess",
            PermissionConstants.PATIENT_VIEW, PermissionConstants.PATIENT_CREATE, PermissionConstants.PATIENT_EDIT,
            PermissionConstants.PATIENT_DELETE, "PatientSearch",
            PermissionConstants.PRESCRIPTION_VIEW, PermissionConstants.PRESCRIPTION_CREATE, PermissionConstants.PRESCRIPTION_EDIT,
            PermissionConstants.PRESCRIPTION_FILL, "PrescriptionDispense",
            PermissionConstants.CLINICAL_TOOLS, PermissionConstants.DRUG_INTERACTIONS, PermissionConstants.ALLERGY_CHECK,
            "DosageCalculator", "ClinicalGuidelines",
            "ReportsView", "ReportsCreate", "ReportsExport",
            "ReportsSchedule", "AnalyticsAccess",
            "ViewDashboard", "AccessSystem"
        };

        // Pharmacist - Clinical and inventory focus
        public static readonly List<string> PharmacistPermissions = new()
        {
            PermissionConstants.INVENTORY_VIEW, PermissionConstants.INVENTORY_CREATE, PermissionConstants.INVENTORY_EDIT,
            PermissionConstants.INVENTORY_EXPORT, "StockAdjust", "LowStockAlert",
            PermissionConstants.SALES_VIEW, PermissionConstants.SALES_CREATE, "ReceiptPrint",
            PermissionConstants.PATIENT_VIEW, PermissionConstants.PATIENT_CREATE, PermissionConstants.PATIENT_EDIT,
            "PatientSearch", PermissionConstants.PRESCRIPTION_VIEW, PermissionConstants.PRESCRIPTION_CREATE,
            PermissionConstants.PRESCRIPTION_EDIT, PermissionConstants.PRESCRIPTION_FILL, "PrescriptionDispense",
            PermissionConstants.CLINICAL_TOOLS, PermissionConstants.DRUG_INTERACTIONS, PermissionConstants.ALLERGY_CHECK,
            "DosageCalculator", "ClinicalGuidelines",
            "ReportsView", "ReportsExport",
            "ViewDashboard", "AccessSystem"
        };

        // Cashier - Sales focused
        public static readonly List<string> CashierPermissions = new()
        {
            PermissionConstants.INVENTORY_VIEW, PermissionConstants.INVENTORY_EXPORT,
            PermissionConstants.SALES_VIEW, PermissionConstants.SALES_CREATE, "ReceiptPrint",
            PermissionConstants.PATIENT_VIEW, "PatientSearch",
            PermissionConstants.PRESCRIPTION_VIEW, PermissionConstants.PRESCRIPTION_FILL, "PrescriptionDispense",
            "ViewDashboard", "AccessSystem"
        };

        // Operations - Operational focus
        public static readonly List<string> OperationsPermissions = new()
        {
            PermissionConstants.INVENTORY_VIEW, PermissionConstants.INVENTORY_CREATE, PermissionConstants.INVENTORY_EDIT,
            PermissionConstants.INVENTORY_IMPORT, PermissionConstants.INVENTORY_EXPORT, "StockAdjust",
            "LowStockAlert", PermissionConstants.SALES_VIEW, PermissionConstants.SALES_CREATE,
            PermissionConstants.SALES_EDIT, PermissionConstants.SALES_REFUND, "ReceiptPrint",
            "DaybookAccess", PermissionConstants.PATIENT_VIEW, "PatientSearch",
            "ReportsView", "ReportsCreate", "ReportsExport",
            "ViewDashboard", "AccessSystem"
        };

        // Reports - Reporting focused
        public static readonly List<string> ReportsPermissions = new()
        {
            PermissionConstants.INVENTORY_VIEW, PermissionConstants.INVENTORY_EXPORT,
            PermissionConstants.SALES_VIEW, "SalesExport",
            PermissionConstants.PATIENT_VIEW, "PatientSearch",
            PermissionConstants.PRESCRIPTION_VIEW,
            "ReportsView", "ReportsCreate", "ReportsExport",
            "ReportsSchedule", "AnalyticsAccess",
            "ViewDashboard", "AccessSystem"
        };
    }

    // Permission service interface
    public interface IPermissionService
    {
        Task<PermissionCheckResult> CheckPermissionAsync(string userId, string permission, string? tenantId = null);
        Task<UserPermissionsSummary> GetUserPermissionsAsync(string userId, string? tenantId = null);
        Task<bool> HasPermissionAsync(string userId, string permission, string? tenantId = null);
        Task<List<string>> GetUserRolePermissionsAsync(string userId, string? tenantId = null);
        Task<bool> IsInRoleAsync(string userId, string role, string? tenantId = null);
        Task SeedDefaultPermissionsAsync();
        Task SeedDefaultRolesAsync();
    }
}
