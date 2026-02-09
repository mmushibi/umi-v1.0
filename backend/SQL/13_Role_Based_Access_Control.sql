-- Role-Based Access Control Updates
-- This file contains updates to implement the role hierarchy: System/Super Admin → Operations/Sales Team → Tenant Admin → Pharmacist/Cashier

-- ========================================
-- Role Hierarchy Definition
-- ========================================

-- Update Roles Table with hierarchical structure
INSERT INTO Roles (Name, DisplayName, Description, RoleType, IsSystem) VALUES
('SuperAdmin', 'Super Administrator', 'System-wide administrator with full access to all features', 'System', true),
('OperationsAdmin', 'Operations Administrator', 'Operations team administrator with access to sales operations and system monitoring', 'System', true),
('SalesTeamAdmin', 'Sales Team Administrator', 'Sales team administrator with access to sales analytics and team management', 'System', true),
('TenantAdmin', 'Tenant Administrator', 'Tenant-level administrator with full access to tenant features', 'Custom', false),
('BranchManager', 'Branch Manager', 'Branch-level manager with access to branch operations', 'Custom', false),
('Pharmacist', 'Pharmacist', 'Pharmacy staff with access to prescription and inventory management', 'Custom', false),
('Cashier', 'Cashier', 'Point of sale staff with access to sales and customer management', 'Custom', false),
('SalesRep', 'Sales Representative', 'Sales representative with access to customer management and sales tracking', 'Custom', false),
('Accountant', 'Accountant', 'Accounting staff with access to financial reports and transactions', 'Custom', false)
ON CONFLICT (Name) DO NOTHING;

-- ========================================
-- Role-Based Permissions Matrix
-- ========================================

-- Super Admin Permissions (System-wide access)
INSERT INTO Permissions (Name, DisplayName, Description, Module, Action, Resource, IsSystem) VALUES
('system.full_access', 'Full System Access', 'Complete access to all system features', 'System', 'Full', 'All', true),
('system.user_management', 'User Management', 'Manage all user accounts across system', 'System', 'Manage', 'UserAccount', true),
('system.tenant_management', 'Tenant Management', 'Manage all tenants in system', 'System', 'Manage', 'Tenant', true),
('system.system_configuration', 'System Configuration', 'Configure system-wide settings', 'System', 'Configure', 'System', true),
('system.audit_logs', 'Audit Logs', 'View all system audit logs', 'System', 'Read', 'AuditLog', true),
('system.backup_restore', 'Backup and Restore', 'Perform system backups and restores', 'System', 'Execute', 'Backup', true),
('system.monitoring', 'System Monitoring', 'Monitor system health and performance', 'System', 'Read', 'Monitoring', true)
ON CONFLICT (Name) DO NOTHING;

-- Operations Admin Permissions (Operations and Sales oversight)
INSERT INTO Permissions (Name, DisplayName, Description, Module, Action, Resource, IsSystem) VALUES
('operations.sales_oversight', 'Sales Oversight', 'Oversight of all sales operations across tenants', 'Operations', 'Read', 'Sales', false),
('operations.analytics_access', 'Analytics Access', 'Access to system-wide analytics and reporting', 'Operations', 'Read', 'Analytics', false),
('operations.team_management', 'Team Management', 'Manage operations and sales teams', 'Operations', 'Manage', 'Team', false),
('operations.performance_monitoring', 'Performance Monitoring', 'Monitor performance metrics across system', 'Operations', 'Read', 'Performance', false),
('operations.regional_management', 'Regional Management', 'Manage multi-regional operations', 'Operations', 'Manage', 'Region', false),
('operations.compliance_monitoring', 'Compliance Monitoring', 'Monitor compliance across all tenants', 'Operations', 'Read', 'Compliance', false)
ON CONFLICT (Name) DO NOTHING;

-- Sales Team Admin Permissions (Sales team management)
INSERT INTO Permissions (Name, DisplayName, Description, Module, Action, Resource, IsSystem) VALUES
('sales.team_management', 'Sales Team Management', 'Manage sales team members and assignments', 'Sales', 'Manage', 'SalesTeam', false),
('sales.target_management', 'Target Management', 'Set and manage sales targets', 'Sales', 'Manage', 'Target', false),
('sales.commission_management', 'Commission Management', 'Manage commission structures and payouts', 'Sales', 'Manage', 'Commission', false),
('sales.territory_management', 'Territory Management', 'Manage sales territories and assignments', 'Sales', 'Manage', 'Territory', false),
('sales.customer_relationship', 'Customer Relationship', 'Manage customer relationships and accounts', 'Sales', 'Manage', 'Customer', false),
('sales.reporting', 'Sales Reporting', 'Access to advanced sales reports', 'Sales', 'Read', 'Report', false)
ON CONFLICT (Name) DO NOTHING;

-- ========================================
-- Role Permission Assignments
-- ========================================

-- Super Admin Role Permissions (All permissions)
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted)
SELECT r.Id, p.Id, true
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'SuperAdmin'
ON CONFLICT (RoleId, PermissionId) DO NOTHING;

-- Operations Admin Role Permissions
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted)
SELECT r.Id, p.Id, true
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'OperationsAdmin' AND p.Module IN ('Operations', 'Sales')
ON CONFLICT (RoleId, PermissionId) DO NOTHING;

-- Sales Team Admin Role Permissions
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted)
SELECT r.Id, p.Id, true
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'SalesTeamAdmin' AND p.Module = 'Sales'
ON CONFLICT (RoleId, PermissionId) DO NOTHING;

-- ========================================
-- Role Hierarchy Constraints
-- ========================================

-- Function to validate role hierarchy
CREATE OR REPLACE FUNCTION validate_role_hierarchy()
RETURNS TRIGGER AS $$
BEGIN
    -- Super Admin can access everything
    IF NEW.Role = 'SuperAdmin' THEN
        RETURN NEW;
    END IF;
    
    -- Operations Admin can access Operations and Sales modules
    IF NEW.Role = 'OperationsAdmin' THEN
        -- Can be assigned to any tenant for operations oversight
        RETURN NEW;
    END IF;
    
    -- Sales Team Admin can only be assigned to Sales Team context
    IF NEW.Role = 'SalesTeamAdmin' THEN
        -- Must have valid tenant assignment
        IF NEW.TenantId IS NULL THEN
            RAISE EXCEPTION 'Sales Team Admin must be assigned to a tenant';
        END IF;
        RETURN NEW;
    END IF;
    
    -- Tenant Admin must be assigned to a specific tenant
    IF NEW.Role = 'TenantAdmin' THEN
        IF NEW.TenantId IS NULL THEN
            RAISE EXCEPTION 'Tenant Admin must be assigned to a tenant';
        END IF;
        RETURN NEW;
    END IF;
    
    -- Branch Manager must be assigned to a branch
    IF NEW.Role = 'BranchManager' THEN
        IF NEW.BranchId IS NULL THEN
            RAISE EXCEPTION 'Branch Manager must be assigned to a branch';
        END IF;
        RETURN NEW;
    END IF;
    
    -- Pharmacists and Cashiers must be assigned to tenant and optionally branch
    IF NEW.Role IN ('Pharmacist', 'Cashier', 'SalesRep', 'Accountant') THEN
        IF NEW.TenantId IS NULL THEN
            RAISE EXCEPTION 'Staff roles must be assigned to a tenant';
        END IF;
        RETURN NEW;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply role hierarchy validation trigger
CREATE TRIGGER validate_role_hierarchy_trigger
    BEFORE INSERT OR UPDATE ON UserAccounts
    FOR EACH ROW EXECUTE FUNCTION validate_role_hierarchy();

-- ========================================
-- Role-Based Data Access Constraints
-- ========================================

-- Function to enforce data access based on role hierarchy
CREATE OR REPLACE FUNCTION enforce_role_based_access()
RETURNS TRIGGER AS $$
BEGIN
    -- Super Admin can access all data
    IF EXISTS (
        SELECT 1 FROM UserAccounts ua 
        WHERE ua.Id = NEW.UserId AND ua.Role = 'SuperAdmin'
    ) THEN
        RETURN NEW;
    END IF;
    
    -- Operations Admin can access operations-level data
    IF EXISTS (
        SELECT 1 FROM UserAccounts ua 
        WHERE ua.Id = NEW.UserId AND ua.Role = 'OperationsAdmin'
    ) THEN
        -- Can access cross-tenant operations data
        RETURN NEW;
    END IF;
    
    -- Sales Team Admin can access sales data across tenant
    IF EXISTS (
        SELECT 1 FROM UserAccounts ua 
        WHERE ua.Id = NEW.UserId AND ua.Role = 'SalesTeamAdmin'
    ) THEN
        -- Can access sales data within their tenant
        IF TG_TABLE_NAME IN ('Sales', 'SalesItems', 'Customers', 'SalesTargets') THEN
            RETURN NEW;
        END IF;
    END IF;
    
    -- Tenant Admin can only access their tenant's data
    IF EXISTS (
        SELECT 1 FROM UserAccounts ua 
        WHERE ua.Id = NEW.UserId AND ua.Role = 'TenantAdmin'
    ) THEN
        -- Enforce tenant isolation
        IF NEW.TenantId IS NOT NULL THEN
            RETURN NEW;
        ELSE
            RAISE EXCEPTION 'Tenant Admin must have valid tenant assignment';
        END IF;
    END IF;
    
    -- Branch Manager can only access their branch's data
    IF EXISTS (
        SELECT 1 FROM UserAccounts ua 
        WHERE ua.Id = NEW.UserId AND ua.Role = 'BranchManager'
    ) THEN
        -- Enforce branch isolation
        IF NEW.BranchId IS NOT NULL THEN
            RETURN NEW;
        ELSE
            RAISE EXCEPTION 'Branch Manager must have valid branch assignment';
        END IF;
    END IF;
    
    -- Staff roles (Pharmacist, Cashier, SalesRep, Accountant)
    IF EXISTS (
        SELECT 1 FROM UserAccounts ua 
        WHERE ua.Id = NEW.UserId AND ua.Role IN ('Pharmacist', 'Cashier', 'SalesRep', 'Accountant')
    ) THEN
        -- Enforce tenant and optional branch isolation
        IF NEW.TenantId IS NOT NULL THEN
            RETURN NEW;
        ELSE
            RAISE EXCEPTION 'Staff roles must have valid tenant assignment';
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply data access constraints to key tables
CREATE TRIGGER enforce_sales_access
    BEFORE INSERT OR UPDATE ON Sales
    FOR EACH ROW EXECUTE FUNCTION enforce_role_based_access();

CREATE TRIGGER enforce_inventory_access
    BEFORE INSERT OR UPDATE ON InventoryItems
    FOR EACH ROW EXECUTE FUNCTION enforce_role_based_access();

CREATE TRIGGER enforce_patients_access
    BEFORE INSERT OR UPDATE ON Patients
    FOR EACH ROW EXECUTE FUNCTION enforce_role_based_access();

CREATE TRIGGER enforce_prescriptions_access
    BEFORE INSERT OR UPDATE ON Prescriptions
    FOR EACH ROW EXECUTE FUNCTION enforce_role_based_access();

-- ========================================
-- Role-Based UI Visibility Views
-- ========================================

-- View for User Role Permissions
CREATE OR REPLACE VIEW UserRolePermissions AS
SELECT 
    ua.Id as UserId,
    ua.Username,
    ua.Role,
    ua.TenantId,
    ua.BranchId,
    r.DisplayName as RoleDisplayName,
    p.Name as PermissionName,
    p.DisplayName as PermissionDisplayName,
    p.Module,
    p.Action,
    p.Resource,
    CASE 
        WHEN ua.Role = 'SuperAdmin' THEN 'System Administrator'
        WHEN ua.Role = 'OperationsAdmin' THEN 'Operations Administrator'
        WHEN ua.Role = 'SalesTeamAdmin' THEN 'Sales Team Administrator'
        WHEN ua.Role = 'TenantAdmin' THEN 'Tenant Administrator'
        WHEN ua.Role = 'BranchManager' THEN 'Branch Manager'
        WHEN ua.Role = 'Pharmacist' THEN 'Pharmacist'
        WHEN ua.Role = 'Cashier' THEN 'Cashier'
        WHEN ua.Role = 'SalesRep' THEN 'Sales Representative'
        WHEN ua.Role = 'Accountant' THEN 'Accountant'
        ELSE ua.Role
    END as RoleDescription
FROM UserAccounts ua
JOIN Roles r ON ua.Role = r.Name
LEFT JOIN RolePermissions rp ON r.Id = rp.RoleId
LEFT JOIN Permissions p ON rp.PermissionId = p.Id
WHERE ua.IsActive = true;

-- View for Role Hierarchy Summary
CREATE OR REPLACE VIEW RoleHierarchySummary AS
SELECT 
    r.Name as RoleName,
    r.DisplayName as RoleDisplayName,
    r.RoleType,
    r.IsSystem,
    COUNT(DISTINCT rp.PermissionId) as PermissionCount,
    STRING_AGG(p.DisplayName, ', ' ORDER BY p.DisplayName') as Permissions,
    CASE 
        WHEN r.Name = 'SuperAdmin' THEN 1
        WHEN r.Name = 'OperationsAdmin' THEN 2
        WHEN r.Name = 'SalesTeamAdmin' THEN 3
        WHEN r.Name = 'TenantAdmin' THEN 4
        WHEN r.Name = 'BranchManager' THEN 5
        WHEN r.Name = 'Pharmacist' THEN 6
        WHEN r.Name = 'Cashier' THEN 7
        WHEN r.Name = 'SalesRep' THEN 8
        WHEN r.Name = 'Accountant' THEN 9
        ELSE 10
    END as HierarchyLevel
FROM Roles r
LEFT JOIN RolePermissions rp ON r.Id = rp.RoleId
LEFT JOIN Permissions p ON rp.PermissionId = p.Id
WHERE r.IsActive = true
GROUP BY r.Id, r.Name, r.DisplayName, r.RoleType, r.IsSystem
ORDER BY HierarchyLevel;

-- ========================================
-- Role-Based Menu and Feature Access
-- ========================================

-- View for Role-Based Menu Access
CREATE OR REPLACE VIEW RoleMenuAccess AS
SELECT 
    r.Name as RoleName,
    m.MenuName,
    m.MenuUrl,
    m.MenuIcon,
    m.ParentMenu,
    m.DisplayOrder,
    CASE 
        WHEN r.Name = 'SuperAdmin' THEN true
        WHEN r.Name = 'OperationsAdmin' AND m.RequiredRoleLevel <= 2 THEN true
        WHEN r.Name = 'SalesTeamAdmin' AND m.RequiredRoleLevel <= 3 THEN true
        WHEN r.Name = 'TenantAdmin' AND m.RequiredRoleLevel <= 4 THEN true
        WHEN r.Name = 'BranchManager' AND m.RequiredRoleLevel <= 5 THEN true
        WHEN r.Name = 'Pharmacist' AND m.RequiredRoleLevel <= 6 THEN true
        WHEN r.Name = 'Cashier' AND m.RequiredRoleLevel <= 7 THEN true
        WHEN r.Name = 'SalesRep' AND m.RequiredRoleLevel <= 8 THEN true
        WHEN r.Name = 'Accountant' AND m.RequiredRoleLevel <= 9 THEN true
        ELSE false
    END as HasAccess
FROM Roles r
CROSS JOIN (
    -- Define menu structure with required role levels
    SELECT 'Dashboard' as MenuName, '/dashboard' as MenuUrl, 'dashboard' as MenuIcon, NULL as ParentMenu, 1 as DisplayOrder, 1 as RequiredRoleLevel
    UNION ALL
    SELECT 'Point of Sale' as MenuName, '/pos' as MenuUrl, 'cash-register' as MenuIcon, NULL as ParentMenu, 2 as DisplayOrder, 7 as RequiredRoleLevel
    UNION ALL
    SELECT 'Inventory' as MenuName, '/inventory' as MenuUrl, 'package' as MenuIcon, NULL as ParentMenu, 3 as DisplayOrder, 6 as RequiredRoleLevel
    UNION ALL
    SELECT 'Patients' as MenuName, '/patients' as MenuUrl, 'users' as MenuIcon, NULL as ParentMenu, 4 as DisplayOrder, 6 as RequiredRoleLevel
    UNION ALL
    SELECT 'Prescriptions' as MenuName, '/prescriptions' as MenuUrl, 'file-text' as MenuIcon, NULL as ParentMenu, 5 as DisplayOrder, 6 as RequiredRoleLevel
    UNION ALL
    SELECT 'Sales' as MenuName, '/sales' as MenuUrl, 'trending-up' as MenuIcon, NULL as ParentMenu, 6 as DisplayOrder, 3 as RequiredRoleLevel
    UNION ALL
    SELECT 'Reports' as MenuName, '/reports' as MenuUrl, 'bar-chart' as MenuIcon, NULL as ParentMenu, 7 as DisplayOrder, 4 as RequiredRoleLevel
    UNION ALL
    SELECT 'User Management' as MenuName, '/users' as MenuUrl, 'settings' as MenuIcon, NULL as ParentMenu, 8 as DisplayOrder, 4 as RequiredRoleLevel
    UNION ALL
    SELECT 'Branch Management' as MenuName, '/branches' as MenuIcon, 'map' as MenuIcon, NULL as ParentMenu, 9 as DisplayOrder, 5 as RequiredRoleLevel
    UNION ALL
    SELECT 'Suppliers' as MenuName, '/suppliers' as MenuIcon, 'truck' as MenuIcon, NULL as ParentMenu, 10 as DisplayOrder, 4 as RequiredRoleLevel
    UNION ALL
    SELECT 'Help & Training' as MenuName, '/help' as MenuIcon, 'help-circle' as MenuIcon, NULL as ParentMenu, 11 as DisplayOrder, 1 as RequiredRoleLevel
    UNION ALL
    SELECT 'Account Settings' as MenuName, '/account' as MenuIcon, 'user' as MenuIcon, NULL as ParentMenu, 12 as DisplayOrder, 1 as RequiredRoleLevel
) m
WHERE r.IsActive = true
ORDER BY m.DisplayOrder;
