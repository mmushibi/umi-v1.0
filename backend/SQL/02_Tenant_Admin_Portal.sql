-- Tenant Admin Portal Tables
-- This file contains SQL for tables primarily used by the Tenant Admin portal

-- ========================================
-- Tenant Admin Specific Tables
-- ========================================

-- Tenants Table (Multi-tenant support)
CREATE TABLE IF NOT EXISTS Tenants (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    BusinessName VARCHAR(200) NOT NULL,
    LicenseNumber VARCHAR(100),
    Address VARCHAR(500),
    Phone VARCHAR(20),
    Email VARCHAR(100),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Users Table (for user management)
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('TenantAdmin', 'Pharmacist', 'Cashier', 'SuperAdmin')),
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    IsActive BOOLEAN DEFAULT true,
    LastLogin TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Sessions Table (for session management)
CREATE TABLE IF NOT EXISTS UserSessions (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    SessionToken VARCHAR(255) UNIQUE NOT NULL,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    ExpiresAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Inventory Management Tables (Tenant Admin)
-- ========================================

-- Inventory Items Table (Zambia-specific pharmaceutical inventory)
CREATE TABLE IF NOT EXISTS InventoryItems (
    Id SERIAL PRIMARY KEY,
    InventoryItemName VARCHAR(200) NOT NULL,
    GenericName VARCHAR(200) NOT NULL,
    BrandName VARCHAR(200) NOT NULL,
    ManufactureDate DATE NOT NULL,
    BatchNumber VARCHAR(100) NOT NULL UNIQUE,
    LicenseNumber VARCHAR(100),
    ZambiaRegNumber VARCHAR(100),
    PackingType VARCHAR(50) NOT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0 CHECK (Quantity >= 0),
    UnitPrice DECIMAL(10,2) NOT NULL CHECK (UnitPrice >= 0),
    SellingPrice DECIMAL(10,2) NOT NULL CHECK (SellingPrice >= 0),
    ReorderLevel INTEGER DEFAULT 10,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Stock Transactions Table (for inventory tracking)
CREATE TABLE IF NOT EXISTS StockTransactions (
    Id SERIAL PRIMARY KEY,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE CASCADE,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE CASCADE,
    TransactionType VARCHAR(50) NOT NULL CHECK (TransactionType IN ('Sale', 'Purchase', 'Adjustment', 'Return')),
    QuantityChange INTEGER NOT NULL,
    PreviousStock INTEGER NOT NULL,
    NewStock INTEGER NOT NULL,
    Reason VARCHAR(200),
    UserId INTEGER REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Reports and Analytics Tables
-- ========================================

-- Sales Reports Table (generated reports)
CREATE TABLE IF NOT EXISTS SalesReports (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    ReportName VARCHAR(200) NOT NULL,
    ReportType VARCHAR(50) NOT NULL, -- 'Daily', 'Weekly', 'Monthly', 'Custom'
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    TotalSales INTEGER NOT NULL DEFAULT 0,
    TotalRevenue DECIMAL(15,2) NOT NULL DEFAULT 0,
    TotalTax DECIMAL(15,2) NOT NULL DEFAULT 0,
    ReportData TEXT, -- JSON data for detailed breakdown
    GeneratedBy INTEGER REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Inventory Reports Table
CREATE TABLE IF NOT EXISTS InventoryReports (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    ReportName VARCHAR(200) NOT NULL,
    ReportType VARCHAR(50) NOT NULL,
    TotalItems INTEGER NOT NULL DEFAULT 0,
    LowStockItems INTEGER NOT NULL DEFAULT 0,
    TotalValue DECIMAL(15,2) NOT NULL DEFAULT 0,
    ReportData TEXT,
    GeneratedBy INTEGER REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- System Configuration Tables
-- ========================================

-- System Settings Table
CREATE TABLE IF NOT EXISTS SystemSettings (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    SettingKey VARCHAR(100) NOT NULL,
    SettingValue TEXT,
    Description VARCHAR(500),
    Category VARCHAR(50),
    IsEditable BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(TenantId, SettingKey)
);

-- Backup Logs Table (for backup tracking)
CREATE TABLE IF NOT EXISTS BackupLogs (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BackupType VARCHAR(50) NOT NULL, -- 'Full', 'Incremental', 'Differential'
    BackupPath VARCHAR(500),
    FileSize BIGINT,
    Status VARCHAR(20) NOT NULL, -- 'Started', 'Completed', 'Failed'
    ErrorMessage TEXT,
    StartedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CompletedAt TIMESTAMP WITH TIME ZONE,
    CreatedBy INTEGER REFERENCES Users(Id)
);

-- ========================================
-- Audit and Security Tables
-- ========================================

-- Audit Logs Table (for tracking all changes)
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    UserId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    Action VARCHAR(100) NOT NULL,
    TableName VARCHAR(50),
    RecordId INTEGER,
    OldValues TEXT,
    NewValues TEXT,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Login Attempts Table (for security monitoring)
CREATE TABLE IF NOT EXISTS LoginAttempts (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    Success BOOLEAN NOT NULL,
    FailureReason VARCHAR(100),
    AttemptTime TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Tenant Admin Tables
-- ========================================

-- Tenants Indexes
CREATE INDEX IF NOT EXISTS idx_tenants_name ON Tenants(Name);
CREATE INDEX IF NOT EXISTS idx_tenants_active ON Tenants(IsActive);

-- Users Indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_role ON Users(Role);
CREATE INDEX IF NOT EXISTS idx_users_tenant ON Users(TenantId);
CREATE INDEX IF NOT EXISTS idx_users_active ON Users(IsActive);

-- User Sessions Indexes
CREATE INDEX IF NOT EXISTS idx_usersessions_user ON UserSessions(UserId);
CREATE INDEX IF NOT EXISTS idx_usersessions_token ON UserSessions(SessionToken);
CREATE INDEX IF NOT EXISTS idx_usersessions_expires ON UserSessions(ExpiresAt);

-- Inventory Items Indexes
CREATE INDEX IF NOT EXISTS idx_inventoryitems_batch ON InventoryItems(BatchNumber);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_zambia_reg ON InventoryItems(ZambiaRegNumber);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_name ON InventoryItems(InventoryItemName);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_active ON InventoryItems(IsActive);

-- Stock Transactions Indexes
CREATE INDEX IF NOT EXISTS idx_stocktransactions_inventory ON StockTransactions(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_stocktransactions_user ON StockTransactions(UserId);
CREATE INDEX IF NOT EXISTS idx_stocktransactions_date ON StockTransactions(CreatedAt);

-- Reports Indexes
CREATE INDEX IF NOT EXISTS idx_salesreports_tenant ON SalesReports(TenantId);
CREATE INDEX IF NOT EXISTS idx_salesreports_date ON SalesReports(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_inventoryreports_tenant ON InventoryReports(TenantId);
CREATE INDEX IF NOT EXISTS idx_inventoryreports_date ON InventoryReports(CreatedAt);

-- System Settings Indexes
CREATE INDEX IF NOT EXISTS idx_systemsettings_tenant ON SystemSettings(TenantId);
CREATE INDEX IF NOT EXISTS idx_systemsettings_key ON SystemSettings(SettingKey);
CREATE INDEX IF NOT EXISTS idx_systemsettings_category ON SystemSettings(Category);

-- Backup Logs Indexes
CREATE INDEX IF NOT EXISTS idx_backuplogs_tenant ON BackupLogs(TenantId);
CREATE INDEX IF NOT EXISTS idx_backuplogs_status ON BackupLogs(Status);
CREATE INDEX IF NOT EXISTS idx_backuplogs_date ON BackupLogs(StartedAt);

-- Audit Logs Indexes
CREATE INDEX IF NOT EXISTS idx_auditlogs_tenant ON AuditLogs(TenantId);
CREATE INDEX IF NOT EXISTS idx_auditlogs_user ON AuditLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_auditlogs_table ON AuditLogs(TableName);
CREATE INDEX IF NOT EXISTS idx_auditlogs_date ON AuditLogs(CreatedAt);

-- Login Attempts Indexes
CREATE INDEX IF NOT EXISTS idx_loginattempts_username ON LoginAttempts(Username);
CREATE INDEX IF NOT EXISTS idx_loginattempts_ip ON LoginAttempts(IpAddress);
CREATE INDEX IF NOT EXISTS idx_loginattempts_time ON LoginAttempts(AttemptTime);

-- ========================================
-- Triggers for Tenant Admin Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to Tenant Admin tables
CREATE TRIGGER update_tenants_updated_at BEFORE UPDATE ON Tenants 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON Users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_systemsettings_updated_at BEFORE UPDATE ON SystemSettings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Audit log trigger function
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO AuditLogs (TenantId, UserId, Action, TableName, RecordId, NewValues)
        VALUES (
            COALESCE(NEW.TenantId, NULL),
            COALESCE(NEW.UserId, NULL),
            'INSERT',
            TG_TABLE_NAME,
            NEW.Id,
            row_to_json(NEW)
        );
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO AuditLogs (TenantId, UserId, Action, TableName, RecordId, OldValues, NewValues)
        VALUES (
            COALESCE(NEW.TenantId, OLD.TenantId, NULL),
            COALESCE(NEW.UserId, OLD.UserId, NULL),
            'UPDATE',
            TG_TABLE_NAME,
            NEW.Id,
            row_to_json(OLD),
            row_to_json(NEW)
        );
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO AuditLogs (TenantId, UserId, Action, TableName, RecordId, OldValues)
        VALUES (
            OLD.TenantId,
            OLD.UserId,
            'DELETE',
            TG_TABLE_NAME,
            OLD.Id,
            row_to_json(OLD)
        );
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Apply audit triggers to key tables
CREATE TRIGGER audit_users_trigger
    AFTER INSERT OR UPDATE OR DELETE ON Users
    FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_inventoryitems_trigger
    AFTER INSERT OR UPDATE OR DELETE ON InventoryItems
    FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();

-- ========================================
-- Views for Tenant Admin Dashboard
-- ========================================

-- View for Tenant Overview
CREATE OR REPLACE VIEW TenantOverview AS
SELECT 
    t.Id as TenantId,
    t.BusinessName,
    t.Name as TenantName,
    COUNT(DISTINCT u.Id) as TotalUsers,
    COUNT(DISTINCT CASE WHEN u.IsActive = true THEN u.Id END) as ActiveUsers,
    COUNT(DISTINCT ii.Id) as TotalInventoryItems,
    COUNT(DISTINCT CASE WHEN ii.IsActive = true THEN ii.Id END) as ActiveInventoryItems,
    COALESCE(SUM(ii.Quantity), 0) as TotalStockValue,
    t.CreatedAt as TenantCreated
FROM Tenants t
LEFT JOIN Users u ON t.Id = u.TenantId
LEFT JOIN InventoryItems ii ON t.Id = ii.TenantId
GROUP BY t.Id, t.BusinessName, t.Name, t.CreatedAt;

-- View for User Activity Summary
CREATE OR REPLACE VIEW UserActivitySummary AS
SELECT 
    u.Id,
    u.Username,
    u.FirstName,
    u.LastName,
    u.Role,
    u.LastLogin,
    COUNT(DISTINCT al.Id) as AuditActions,
    COUNT(DISTINCT CASE WHEN al.CreatedAt > CURRENT_DATE - INTERVAL '7 days' THEN al.Id END) as ActionsLast7Days,
    COUNT(DISTINCT la.Id) as LoginAttempts,
    COUNT(DISTINCT CASE WHEN la.Success = true THEN la.Id END) as SuccessfulLogins
FROM Users u
LEFT JOIN AuditLogs al ON u.Id = al.UserId
LEFT JOIN LoginAttempts la ON u.Username = la.Username
GROUP BY u.Id, u.Username, u.FirstName, u.LastName, u.Role, u.LastLogin;
