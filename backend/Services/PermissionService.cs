using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public PermissionService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<PermissionService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PermissionCheckResult> CheckPermissionAsync(string userId, string permission, string? tenantId = null)
        {
            try
            {
                var cacheKey = $"permission_check_{userId}_{permission}_{tenantId}";

                if (_cache.TryGetValue(cacheKey, out PermissionCheckResult? cachedResult))
                {
                    return cachedResult!;
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId || u.Id.ToString() == userId);

                if (user == null)
                {
                    var result = new PermissionCheckResult
                    {
                        IsAllowed = false,
                        Permission = permission,
                        UserId = userId,
                        Role = "Unknown",
                        Reason = "User not found"
                    };

                    _cache.Set(cacheKey, result, _cacheDuration);
                    return result;
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    var result = new PermissionCheckResult
                    {
                        IsAllowed = false,
                        Permission = permission,
                        UserId = userId,
                        Role = user.Role,
                        Reason = "User account is not active"
                    };

                    _cache.Set(cacheKey, result, _cacheDuration);
                    return result;
                }

                // Get user's permissions based on role
                var userPermissions = await GetUserRolePermissionsAsync(userId, tenantId);
                var hasPermission = userPermissions.Contains(permission);

                var checkResult = new PermissionCheckResult
                {
                    IsAllowed = hasPermission,
                    Permission = permission,
                    UserId = userId,
                    Role = user.Role,
                    Reason = hasPermission ? "Permission granted" : "Permission denied"
                };

                _cache.Set(cacheKey, checkResult, _cacheDuration);
                return checkResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);

                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    Permission = permission,
                    UserId = userId,
                    Role = "Unknown",
                    Reason = "Error occurred while checking permission"
                };
            }
        }

        public async Task<UserPermissionsSummary> GetUserPermissionsAsync(string userId, string? tenantId = null)
        {
            try
            {
                var cacheKey = $"user_permissions_{userId}_{tenantId}";

                if (_cache.TryGetValue(cacheKey, out UserPermissionsSummary? cachedSummary))
                {
                    return cachedSummary!;
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId || u.Id.ToString() == userId);

                if (user == null)
                {
                    return new UserPermissionsSummary
                    {
                        UserId = userId,
                        Role = "Unknown",
                        Permissions = new List<Permission>(),
                        Roles = new List<Role>()
                    };
                }

                // Get roles from database based on user role
                var roles = await _context.Roles
                    .Where(r => r.Name == user.Role)
                    .ToListAsync();

                var permissions = await _context.Permissions
                    .Join(_context.RolePermissions,
                        p => p.Id,
                        rp => rp.PermissionId,
                        (p, rp) => new { p, rp })
                    .Join(_context.Roles,
                        combined => combined.rp.RoleId,
                        r => r.Id,
                        (combined, r) => new { combined.p, r })
                    .Where(x => x.r.Name == user.Role)
                    .Select(x => x.p)
                    .ToListAsync();

                var summary = new UserPermissionsSummary
                {
                    UserId = userId,
                    Role = user.Role,
                    Permissions = permissions,
                    Roles = roles
                };

                _cache.Set(cacheKey, summary, _cacheDuration);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);

                return new UserPermissionsSummary
                {
                    UserId = userId,
                    Role = "Unknown",
                    Permissions = new List<Permission>(),
                    Roles = new List<Role>()
                };
            }
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission, string? tenantId = null)
        {
            var result = await CheckPermissionAsync(userId, permission, tenantId);
            return result.IsAllowed;
        }

        public async Task<List<string>> GetUserRolePermissionsAsync(string userId, string? tenantId = null)
        {
            try
            {
                var cacheKey = $"role_permissions_{userId}_{tenantId}";

                if (_cache.TryGetValue(cacheKey, out List<string>? cachedPermissions))
                {
                    return cachedPermissions!;
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId || u.Id.ToString() == userId);

                if (user == null)
                {
                    return new List<string>();
                }

                // Get permissions based on user role
                var rolePermissions = user.Role switch
                {
                    "SuperAdmin" => SystemRolePermissions.SuperAdminPermissions,
                    "TenantAdmin" => SystemRolePermissions.TenantAdminPermissions,
                    "Pharmacist" => SystemRolePermissions.PharmacistPermissions,
                    "Cashier" => SystemRolePermissions.CashierPermissions,
                    "Operations" => SystemRolePermissions.OperationsPermissions,
                    "Reports" => SystemRolePermissions.ReportsPermissions,
                    _ => new List<string>()
                };

                // Also get any additional permissions from database
                var dbPermissions = await _context.RolePermissions
                    .Join(_context.Roles,
                        rp => rp.RoleId,
                        r => r.Id,
                        (rp, r) => new { rp, r })
                    .Join(_context.Permissions,
                        combined => combined.rp.PermissionId,
                        p => p.Id,
                        (combined, p) => new { combined.r, p })
                    .Where(x => x.r.Name == user.Role)
                    .Select(x => x.p.Name)
                    .ToListAsync();

                var allPermissions = rolePermissions.Concat(dbPermissions).Distinct().ToList();

                _cache.Set(cacheKey, allPermissions, _cacheDuration);
                return allPermissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permissions for user {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<bool> IsInRoleAsync(string userId, string role, string? tenantId = null)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId || u.Id.ToString() == userId);

                if (user == null)
                {
                    return false;
                }

                return user.Role.Equals(role, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} is in role {Role}", userId, role);
                return false;
            }
        }

        public async Task SeedDefaultPermissionsAsync()
        {
            try
            {
                var existingPermissions = await _context.Permissions.ToListAsync();

                var defaultPermissions = new List<Permission>
                {
                    // General permissions
                    new() { Name = "ViewDashboard", DisplayName = "View Dashboard", Category = "General", Description = "Access main dashboard" },
                    new() { Name = "AccessSystem", DisplayName = "Access System", Category = "General", Description = "Access the system" },

                    // Inventory permissions
                    new() { Name = "InventoryView", DisplayName = "View Inventory", Category = "Inventory", Description = "View inventory items" },
                    new() { Name = "InventoryCreate", DisplayName = "Create Inventory", Category = "Inventory", Description = "Create new inventory items" },
                    new() { Name = "InventoryEdit", DisplayName = "Edit Inventory", Category = "Inventory", Description = "Edit inventory items" },
                    new() { Name = "InventoryDelete", DisplayName = "Delete Inventory", Category = "Inventory", Description = "Delete inventory items" },
                    new() { Name = "InventoryImport", DisplayName = "Import Inventory", Category = "Inventory", Description = "Import inventory from CSV" },
                    new() { Name = "InventoryExport", DisplayName = "Export Inventory", Category = "Inventory", Description = "Export inventory to CSV" },
                    new() { Name = "StockAdjust", DisplayName = "Stock Adjustment", Category = "Inventory", Description = "Adjust stock levels" },
                    new() { Name = "LowStockAlert", DisplayName = "Low Stock Alert", Category = "Inventory", Description = "View low stock alerts" },

                    // Sales permissions
                    new() { Name = "SalesView", DisplayName = "View Sales", Category = "Sales", Description = "View sales records" },
                    new() { Name = "SalesCreate", DisplayName = "Create Sales", Category = "Sales", Description = "Create new sales" },
                    new() { Name = "SalesEdit", DisplayName = "Edit Sales", Category = "Sales", Description = "Edit sales records" },
                    new() { Name = "SalesDelete", DisplayName = "Delete Sales", Category = "Sales", Description = "Delete sales records" },
                    new() { Name = "SalesRefund", DisplayName = "Refund Sales", Category = "Sales", Description = "Process refunds" },
                    new() { Name = "ReceiptPrint", DisplayName = "Print Receipt", Category = "Sales", Description = "Print receipts" },
                    new() { Name = "DaybookAccess", DisplayName = "Access Daybook", Category = "Sales", Description = "Access daybook" },

                    // Patient permissions
                    new() { Name = "PatientView", DisplayName = "View Patients", Category = "Patients", Description = "View patient records" },
                    new() { Name = "PatientCreate", DisplayName = "Create Patients", Category = "Patients", Description = "Create new patient records" },
                    new() { Name = "PatientEdit", DisplayName = "Edit Patients", Category = "Patients", Description = "Edit patient records" },
                    new() { Name = "PatientDelete", DisplayName = "Delete Patients", Category = "Patients", Description = "Delete patient records" },
                    new() { Name = "PatientSearch", DisplayName = "Search Patients", Category = "Patients", Description = "Search patient records" },

                    // Prescription permissions
                    new() { Name = "PrescriptionView", DisplayName = "View Prescriptions", Category = "Prescriptions", Description = "View prescription records" },
                    new() { Name = "PrescriptionCreate", DisplayName = "Create Prescriptions", Category = "Prescriptions", Description = "Create new prescriptions" },
                    new() { Name = "PrescriptionEdit", DisplayName = "Edit Prescriptions", Category = "Prescriptions", Description = "Edit prescription records" },
                    new() { Name = "PrescriptionDelete", DisplayName = "Delete Prescriptions", Category = "Prescriptions", Description = "Delete prescription records" },
                    new() { Name = "PrescriptionFill", DisplayName = "Fill Prescriptions", Category = "Prescriptions", Description = "Fill prescriptions" },
                    new() { Name = "PrescriptionDispense", DisplayName = "Dispense Prescriptions", Category = "Prescriptions", Description = "Dispense medications" },

                    // Clinical permissions
                    new() { Name = "ClinicalTools", DisplayName = "Clinical Tools", Category = "Clinical", Description = "Access clinical tools" },
                    new() { Name = "DrugInteractions", DisplayName = "Drug Interactions", Category = "Clinical", Description = "Check drug interactions" },
                    new() { Name = "AllergyCheck", DisplayName = "Allergy Check", Category = "Clinical", Description = "Check for allergies" },
                    new() { Name = "DosageCalculator", DisplayName = "Dosage Calculator", Category = "Clinical", Description = "Calculate medication dosages" },
                    new() { Name = "ClinicalGuidelines", DisplayName = "Clinical Guidelines", Category = "Clinical", Description = "View clinical guidelines" },

                    // Reports permissions
                    new() { Name = "ReportsView", DisplayName = "View Reports", Category = "Reports", Description = "View reports" },
                    new() { Name = "ReportsCreate", DisplayName = "Create Reports", Category = "Reports", Description = "Create new reports" },
                    new() { Name = "ReportsExport", DisplayName = "Export Reports", Category = "Reports", Description = "Export reports" },
                    new() { Name = "ReportsSchedule", DisplayName = "Schedule Reports", Category = "Reports", Description = "Schedule automated reports" },
                    new() { Name = "AnalyticsAccess", DisplayName = "Access Analytics", Category = "Reports", Description = "Access analytics dashboard" },

                    // User management permissions
                    new() { Name = "UserView", DisplayName = "View Users", Category = "Users", Description = "View user accounts" },
                    new() { Name = "UserCreate", DisplayName = "Create Users", Category = "Users", Description = "Create new user accounts" },
                    new() { Name = "UserEdit", DisplayName = "Edit Users", Category = "Users", Description = "Edit user accounts" },
                    new() { Name = "UserDelete", DisplayName = "Delete Users", Category = "Users", Description = "Delete user accounts" },
                    new() { Name = "UserRoles", DisplayName = "Manage User Roles", Category = "Users", Description = "Manage user roles" },
                    new() { Name = "UserPermissions", DisplayName = "Manage User Permissions", Category = "Users", Description = "Manage user permissions" },

                    // Settings permissions
                    new() { Name = "SettingsView", DisplayName = "View Settings", Category = "Settings", Description = "View system settings" },
                    new() { Name = "SettingsEdit", DisplayName = "Edit Settings", Category = "Settings", Description = "Edit system settings" },
                    new() { Name = "SystemConfig", DisplayName = "System Configuration", Category = "Settings", Description = "Configure system settings" },
                    new() { Name = "BranchManage", DisplayName = "Manage Branches", Category = "Settings", Description = "Manage branches" },
                    new() { Name = "SupplierManage", DisplayName = "Manage Suppliers", Category = "Settings", Description = "Manage suppliers" },
                    new() { Name = "CategoryManage", DisplayName = "Manage Categories", Category = "Settings", Description = "Manage categories" },

                    // System permissions
                    new() { Name = "SystemAdmin", DisplayName = "System Administration", Category = "System", Description = "Full system administration" },
                    new() { Name = "TenantAdmin", DisplayName = "Tenant Administration", Category = "System", Description = "Tenant administration" },
                    new() { Name = "Impersonate", DisplayName = "Impersonate Users", Category = "System", Description = "Impersonate other users" },
                    new() { Name = "AuditLog", DisplayName = "View Audit Log", Category = "System", Description = "View audit logs" },
                    new() { Name = "Backup", DisplayName = "System Backup", Category = "System", Description = "Perform system backup" },
                    new() { Name = "Restore", DisplayName = "System Restore", Category = "System", Description = "Perform system restore" }
                };

                foreach (var permission in defaultPermissions)
                {
                    if (!existingPermissions.Any(p => p.Name == permission.Name))
                    {
                        permission.IsSystem = true;
                        permission.CreatedAt = DateTime.UtcNow;
                        permission.UpdatedAt = DateTime.UtcNow;
                        _context.Permissions.Add(permission);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded default permissions successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default permissions");
                throw;
            }
        }

        public async Task SeedDefaultRolesAsync()
        {
            try
            {
                var existingRoles = await _context.Roles.ToListAsync();
                var allPermissions = await _context.Permissions.ToListAsync();

                var defaultRoles = new List<Role>
                {
                    new() { Name = "SuperAdmin", Description = "Super Administrator with full system access", Level = "High", IsSystem = true },
                    new() { Name = "TenantAdmin", Description = "Tenant Administrator with tenant-level access", Level = "High", IsSystem = true },
                    new() { Name = "Pharmacist", Description = "Pharmacist with clinical and inventory access", Level = "Medium", IsSystem = true },
                    new() { Name = "Cashier", Description = "Cashier with sales-focused access", Level = "Low", IsSystem = true },
                    new() { Name = "Operations", Description = "Operations staff with operational access", Level = "Medium", IsSystem = true },
                    new() { Name = "Reports", Description = "Reports staff with reporting access", Level = "Low", IsSystem = true }
                };

                foreach (var role in defaultRoles)
                {
                    if (!existingRoles.Any(r => r.Name == role.Name))
                    {
                        role.CreatedAt = DateTime.UtcNow;
                        role.UpdatedAt = DateTime.UtcNow;
                        _context.Roles.Add(role);
                    }
                }

                await _context.SaveChangesAsync();

                // Refresh roles with IDs
                var savedRoles = await _context.Roles.ToListAsync();

                // Assign permissions to roles
                var rolePermissionsMap = new Dictionary<string, List<string>>
                {
                    ["SuperAdmin"] = SystemRolePermissions.SuperAdminPermissions,
                    ["TenantAdmin"] = SystemRolePermissions.TenantAdminPermissions,
                    ["Pharmacist"] = SystemRolePermissions.PharmacistPermissions,
                    ["Cashier"] = SystemRolePermissions.CashierPermissions,
                    ["Operations"] = SystemRolePermissions.OperationsPermissions,
                    ["Reports"] = SystemRolePermissions.ReportsPermissions
                };

                foreach (var role in savedRoles)
                {
                    if (rolePermissionsMap.TryGetValue(role.Name, out var permissionNames))
                    {
                        foreach (var permissionName in permissionNames)
                        {
                            var permission = allPermissions.FirstOrDefault(p => p.Name == permissionName);
                            if (permission != null)
                            {
                                var existingRolePermission = await _context.RolePermissions
                                    .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

                                if (existingRolePermission == null)
                                {
                                    var roleEntity = savedRoles.FirstOrDefault(r => r.Id == role.Id);
                                    var permissionEntity = allPermissions.FirstOrDefault(p => p.Id == permission.Id);

                                    _context.RolePermissions.Add(new RolePermission
                                    {
                                        RoleId = role.Id,
                                        PermissionId = permission.Id,
                                        CreatedAt = DateTime.UtcNow,
                                        Role = roleEntity!,
                                        Permission = permissionEntity!
                                    });
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded default roles and permissions successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default roles");
                throw;
            }
        }

        public void ClearUserPermissionCache(string userId)
        {
            // Clear specific cache entries related to the user
            // Note: IMemoryCache doesn't provide GetCurrentKeys method, so we'll handle this differently
            // In a real implementation, you might use a distributed cache with key enumeration
            // or maintain a separate registry of cache keys

            // Clear common permission cache patterns for this user
            var patterns = new[]
            {
                $"permission_check_{userId}_",
                $"user_permissions_{userId}_",
                $"role_permissions_{userId}_"
            };

            // This is a simplified approach - in production, consider using a cache
            // that supports key enumeration or maintain a key registry
            foreach (var pattern in patterns)
            {
                // In a real implementation, you would iterate through cache keys
                // For now, we'll rely on cache expiration
                _logger.LogDebug("Would clear cache keys matching pattern: {Pattern}", pattern);
            }
        }
    }
}
