-- Super Admin Portal Tables
-- This file contains SQL for tables primarily used by the Super Admin portal

-- ========================================
-- System Administration Tables
-- ========================================

-- System Configuration Table (global settings)
CREATE TABLE IF NOT EXISTS SystemConfiguration (
    Id SERIAL PRIMARY KEY,
    ConfigKey VARCHAR(100) UNIQUE NOT NULL,
    ConfigValue TEXT,
    ConfigType VARCHAR(20) DEFAULT 'String' CHECK (ConfigType IN ('String', 'Integer', 'Decimal', 'Boolean', 'JSON')),
    Description VARCHAR(500),
    Category VARCHAR(50) DEFAULT 'General',
    IsEditable BOOLEAN DEFAULT true,
    RequiresRestart BOOLEAN DEFAULT false,
    ValidationRule VARCHAR(200),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- System Logs Table (application-wide logging)
CREATE TABLE IF NOT EXISTS SystemLogs (
    Id SERIAL PRIMARY KEY,
    LogLevel VARCHAR(10) NOT NULL CHECK (LogLevel IN ('DEBUG', 'INFO', 'WARN', 'ERROR', 'FATAL')),
    Message TEXT NOT NULL,
    ExceptionDetails TEXT,
    Source VARCHAR(100),
    UserId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    RequestId VARCHAR(50),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- System Maintenance Table (maintenance windows and notifications)
CREATE TABLE IF NOT EXISTS SystemMaintenance (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Description TEXT,
    MaintenanceType VARCHAR(20) NOT NULL CHECK (MaintenanceType IN ('Scheduled', 'Emergency', 'Routine')),
    ScheduledStart TIMESTAMP WITH TIME ZONE NOT NULL,
    ScheduledEnd TIMESTAMP WITH TIME ZONE NOT NULL,
    ActualStart TIMESTAMP WITH TIME ZONE,
    ActualEnd TIMESTAMP WITH TIME ZONE,
    Status VARCHAR(20) DEFAULT 'Scheduled' CHECK (Status IN ('Scheduled', 'InProgress', 'Completed', 'Cancelled')),
    AffectedSystems TEXT, -- JSON array of affected systems/modules
    NotificationSent BOOLEAN DEFAULT false,
    CreatedBy INTEGER REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Multi-Tenant Management Tables
-- ========================================

-- Tenants Table (extended for super admin management)
CREATE TABLE IF NOT EXISTS Tenants (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    BusinessName VARCHAR(200) NOT NULL,
    BusinessRegistrationNumber VARCHAR(100),
    TaxIdentificationNumber VARCHAR(50),
    LicenseNumber VARCHAR(100),
    Address VARCHAR(500),
    City VARCHAR(100),
    Country VARCHAR(100) DEFAULT 'Zambia',
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Website VARCHAR(200),
    Industry VARCHAR(100),
    CompanySize VARCHAR(20) CHECK (CompanySize IN ('Small', 'Medium', 'Large', 'Enterprise')),
    SubscriptionPlanId INTEGER REFERENCES SubscriptionPlans(Id) ON DELETE SET NULL,
    MaxUsers INTEGER DEFAULT 10,
    MaxProducts INTEGER DEFAULT 1000,
    MaxTransactionsPerMonth INTEGER DEFAULT 10000,
    StorageLimitGB INTEGER DEFAULT 10,
    IsActive BOOLEAN DEFAULT true,
    IsApproved BOOLEAN DEFAULT false,
    ApprovedBy INTEGER REFERENCES Users(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    SubscriptionStartDate DATE,
    SubscriptionEndDate DATE,
    TrialEndDate DATE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tenant Settings Table (tenant-specific configurations)
CREATE TABLE IF NOT EXISTS TenantSettings (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    SettingKey VARCHAR(100) NOT NULL,
    SettingValue TEXT,
    SettingType VARCHAR(20) DEFAULT 'String',
    Category VARCHAR(50) DEFAULT 'General',
    IsEditable BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(TenantId, SettingKey)
);

-- Tenant Usage Statistics Table (tracking tenant resource usage)
CREATE TABLE IF NOT EXISTS TenantUsageStatistics (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    StatisticsDate DATE NOT NULL,
    ActiveUsers INTEGER DEFAULT 0,
    TotalUsers INTEGER DEFAULT 0,
    ActiveProducts INTEGER DEFAULT 0,
    TotalProducts INTEGER DEFAULT 0,
    TotalTransactions INTEGER DEFAULT 0,
    TotalRevenue DECIMAL(15,2) DEFAULT 0.00,
    StorageUsedGB DECIMAL(10,2) DEFAULT 0.00,
    ApiCalls INTEGER DEFAULT 0,
    BandwidthUsageMB DECIMAL(10,2) DEFAULT 0.00,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(TenantId, StatisticsDate)
);

-- ========================================
-- User Management and Security Tables
-- ========================================

-- Users Table (extended for super admin management)
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('SuperAdmin', 'TenantAdmin', 'Pharmacist', 'Cashier')),
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    PhoneNumber VARCHAR(20),
    ProfilePicture VARCHAR(500),
    LastLogin TIMESTAMP WITH TIME ZONE,
    LoginCount INTEGER DEFAULT 0,
    FailedLoginAttempts INTEGER DEFAULT 0,
    IsLocked BOOLEAN DEFAULT false,
    LockoutUntil TIMESTAMP WITH TIME ZONE,
    PasswordResetToken VARCHAR(255),
    PasswordResetExpires TIMESTAMP WITH TIME ZONE,
    EmailVerified BOOLEAN DEFAULT false,
    EmailVerificationToken VARCHAR(255),
    TwoFactorEnabled BOOLEAN DEFAULT false,
    TwoFactorSecret VARCHAR(255),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Permissions Table (granular permissions)
CREATE TABLE IF NOT EXISTS UserPermissions (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    PermissionKey VARCHAR(100) NOT NULL,
    PermissionName VARCHAR(200) NOT NULL,
    Resource VARCHAR(100) NOT NULL, -- e.g., 'Inventory', 'Sales', 'Reports'
    Action VARCHAR(50) NOT NULL, -- e.g., 'Create', 'Read', 'Update', 'Delete'
    IsGranted BOOLEAN DEFAULT true,
    GrantedBy INTEGER REFERENCES Users(Id),
    GrantedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt TIMESTAMP WITH TIME ZONE,
    UNIQUE(UserId, PermissionKey)
);

-- User Activity Logs Table (detailed user activity tracking)
CREATE TABLE IF NOT EXISTS UserActivityLogs (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    ActivityType VARCHAR(50) NOT NULL, -- 'Login', 'Logout', 'Create', 'Update', 'Delete', 'View'
    ResourceType VARCHAR(50),
    ResourceId INTEGER,
    ResourceName VARCHAR(200),
    Action VARCHAR(100) NOT NULL,
    Details TEXT,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    SessionId VARCHAR(100),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- System Health and Monitoring Tables
-- ========================================

-- System Health Checks Table
CREATE TABLE IF NOT EXISTS SystemHealthChecks (
    Id SERIAL PRIMARY KEY,
    CheckName VARCHAR(100) NOT NULL,
    CheckType VARCHAR(50) NOT NULL, -- 'Database', 'API', 'External Service', 'Disk Space', etc.
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Healthy', 'Warning', 'Critical', 'Unknown')),
    ResponseTime INTEGER, -- in milliseconds
    ErrorMessage TEXT,
    CheckDetails TEXT, -- JSON with additional details
    LastChecked TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- System Metrics Table (performance metrics)
CREATE TABLE IF NOT EXISTS SystemMetrics (
    Id SERIAL PRIMARY KEY,
    MetricName VARCHAR(100) NOT NULL,
    MetricType VARCHAR(50) NOT NULL, -- 'CPU', 'Memory', 'Disk', 'Network', 'Database'
    MetricValue DECIMAL(15,2) NOT NULL,
    Unit VARCHAR(20), -- '%', 'GB', 'MB/s', etc.
    ThresholdWarning DECIMAL(15,2),
    ThresholdCritical DECIMAL(15,2),
    ServerName VARCHAR(100),
    RecordedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Api Usage Analytics Table
CREATE TABLE IF NOT EXISTS ApiUsageAnalytics (
    Id SERIAL PRIMARY KEY,
    Endpoint VARCHAR(200) NOT NULL,
    HttpMethod VARCHAR(10) NOT NULL,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    UserId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    RequestCount INTEGER DEFAULT 1,
    AverageResponseTime INTEGER, -- in milliseconds
    ErrorCount INTEGER DEFAULT 0,
    StatusCodeDistribution TEXT, -- JSON with status code counts
    DateRecorded DATE NOT NULL,
    HourRecorded INTEGER NOT NULL CHECK (HourRecorded >= 0 AND HourRecorded <= 23),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(Endpoint, HttpMethod, TenantId, DateRecorded, HourRecorded)
);

-- ========================================
-- Backup and Disaster Recovery Tables
-- ========================================

-- Backup Jobs Table
CREATE TABLE IF NOT EXISTS BackupJobs (
    Id SERIAL PRIMARY KEY,
    JobName VARCHAR(100) NOT NULL,
    BackupType VARCHAR(20) NOT NULL CHECK (BackupType IN ('Full', 'Incremental', 'Differential')),
    BackupScope VARCHAR(50) NOT NULL, -- 'System', 'Tenant', 'Database', 'Files'
    TargetTenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    ScheduleType VARCHAR(20) NOT NULL CHECK (ScheduleType IN ('Manual', 'Daily', 'Weekly', 'Monthly')),
    ScheduleExpression VARCHAR(100), -- Cron expression
    RetentionDays INTEGER DEFAULT 30,
    BackupLocation VARCHAR(500),
    CompressionEnabled BOOLEAN DEFAULT true,
    EncryptionEnabled BOOLEAN DEFAULT true,
    IsActive BOOLEAN DEFAULT true,
    LastRunAt TIMESTAMP WITH TIME ZONE,
    NextRunAt TIMESTAMP WITH TIME ZONE,
    CreatedBy INTEGER REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Backup Executions Table (backup job execution history)
CREATE TABLE IF NOT EXISTS BackupExecutions (
    Id SERIAL PRIMARY KEY,
    BackupJobId INTEGER NOT NULL REFERENCES BackupJobs(Id) ON DELETE CASCADE,
    ExecutionId VARCHAR(100) UNIQUE NOT NULL,
    StartedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CompletedAt TIMESTAMP WITH TIME ZONE,
    Status VARCHAR(20) DEFAULT 'Running' CHECK (Status IN ('Running', 'Completed', 'Failed', 'Cancelled')),
    BackupSize BIGINT,
    CompressedSize BIGINT,
    FilesBackedUp INTEGER DEFAULT 0,
    BackupPath VARCHAR(500),
    Checksum VARCHAR(255),
    ErrorMessage TEXT,
    VerificationStatus VARCHAR(20), -- 'Pending', 'Verified', 'Failed'
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Compliance and Audit Tables
-- ========================================

-- Compliance Reports Table
CREATE TABLE IF NOT EXISTS ComplianceReports (
    Id SERIAL PRIMARY KEY,
    ReportName VARCHAR(200) NOT NULL,
    ComplianceType VARCHAR(50) NOT NULL, -- 'GDPR', 'HIPAA', 'SOX', 'Zambia Pharmacy Act'
    ReportPeriod VARCHAR(50) NOT NULL, -- e.g., '2024-Q1', '2024-01'
    Scope VARCHAR(100), -- 'System', 'Tenant', 'User'
    TargetId INTEGER, -- ID of the scope target
    Status VARCHAR(20) DEFAULT 'In Progress' CHECK (Status IN ('In Progress', 'Completed', 'Failed', 'Reviewed')),
    Findings TEXT, -- JSON with compliance findings
    RiskLevel VARCHAR(20) CHECK (RiskLevel IN ('Low', 'Medium', 'High', 'Critical')),
    Recommendations TEXT,
    ReportDocumentPath VARCHAR(500),
    ReviewedBy INTEGER REFERENCES Users(Id),
    ReviewedAt TIMESTAMP WITH TIME ZONE,
    GeneratedBy INTEGER REFERENCES Users(Id),
    GeneratedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Data Retention Policies Table
CREATE TABLE IF NOT EXISTS DataRetentionPolicies (
    Id SERIAL PRIMARY KEY,
    PolicyName VARCHAR(100) NOT NULL,
    TableName VARCHAR(50) NOT NULL,
    RetentionPeriod INTEGER NOT NULL, -- in days
    RetentionUnit VARCHAR(20) DEFAULT 'Days' CHECK (RetentionUnit IN ('Days', 'Months', 'Years')),
    ArchivalAction VARCHAR(50) DEFAULT 'Delete' CHECK (ArchivalAction IN ('Delete', 'Archive', 'Anonymize')),
    ArchiveLocation VARCHAR(500),
    IsActive BOOLEAN DEFAULT true,
    LastExecuted TIMESTAMP WITH TIME ZONE,
    NextExecution TIMESTAMP WITH TIME ZONE,
    CreatedBy INTEGER REFERENCES Users(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Super Admin Tables
-- ========================================

-- System Configuration Indexes
CREATE INDEX IF NOT EXISTS idx_systemconfig_key ON SystemConfiguration(ConfigKey);
CREATE INDEX IF NOT EXISTS idx_systemconfig_category ON SystemConfiguration(Category);
CREATE INDEX IF NOT EXISTS idx_systemconfig_editable ON SystemConfiguration(IsEditable);

-- System Logs Indexes
CREATE INDEX IF NOT EXISTS idx_systemlogs_level ON SystemLogs(LogLevel);
CREATE INDEX IF NOT EXISTS idx_systemlogs_user ON SystemLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_systemlogs_tenant ON SystemLogs(TenantId);
CREATE INDEX IF NOT EXISTS idx_systemlogs_date ON SystemLogs(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_systemlogs_source ON SystemLogs(Source);

-- System Maintenance Indexes
CREATE INDEX IF NOT EXISTS idx_systemmaintenance_type ON SystemMaintenance(MaintenanceType);
CREATE INDEX IF NOT EXISTS idx_systemmaintenance_dates ON SystemMaintenance(ScheduledStart, ScheduledEnd);
CREATE INDEX IF NOT EXISTS idx_systemmaintenance_status ON SystemMaintenance(Status);

-- Tenants Indexes
CREATE INDEX IF NOT EXISTS idx_tenants_name ON Tenants(Name);
CREATE INDEX IF NOT EXISTS idx_tenants_business ON Tenants(BusinessName);
CREATE INDEX IF NOT EXISTS idx_tenants_active ON Tenants(IsActive);
CREATE INDEX IF NOT EXISTS idx_tenants_approved ON Tenants(IsApproved);
CREATE INDEX IF NOT EXISTS idx_tenants_subscription ON Tenants(SubscriptionPlanId);

-- Tenant Settings Indexes
CREATE INDEX IF NOT EXISTS idx_tenantsettings_tenant ON TenantSettings(TenantId);
CREATE INDEX IF NOT EXISTS idx_tenantsettings_key ON TenantSettings(SettingKey);
CREATE INDEX IF NOT EXISTS idx_tenantsettings_category ON TenantSettings(Category);

-- Tenant Usage Statistics Indexes
CREATE INDEX IF NOT EXISTS idx_tenantusage_tenant ON TenantUsageStatistics(TenantId);
CREATE INDEX IF NOT EXISTS idx_tenantusage_date ON TenantUsageStatistics(StatisticsDate);

-- Users Indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_role ON Users(Role);
CREATE INDEX IF NOT EXISTS idx_users_tenant ON Users(TenantId);
CREATE INDEX IF NOT EXISTS idx_users_branch ON Users(BranchId);
CREATE INDEX IF NOT EXISTS idx_users_active ON Users(IsActive);
CREATE INDEX IF NOT EXISTS idx_users_locked ON Users(IsLocked);
CREATE INDEX IF NOT EXISTS idx_users_lastlogin ON Users(LastSuccessfulLogin);

-- User Permissions Indexes
CREATE INDEX IF NOT EXISTS idx_userpermissions_user ON UserPermissions(UserId);
CREATE INDEX IF NOT EXISTS idx_userpermissions_key ON UserPermissions(PermissionKey);
CREATE INDEX IF NOT EXISTS idx_userpermissions_resource ON UserPermissions(Resource);

-- User Activity Logs Indexes
CREATE INDEX IF NOT EXISTS idx_useractivitylogs_user ON UserActivityLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_useractivitylogs_tenant ON UserActivityLogs(TenantId);
CREATE INDEX IF NOT EXISTS idx_useractivitylogs_type ON UserActivityLogs(ActivityType);
CREATE INDEX IF NOT EXISTS idx_useractivitylogs_date ON UserActivityLogs(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_useractivitylogs_resource ON UserActivityLogs(ResourceType, ResourceId);

-- System Health Checks Indexes
CREATE INDEX IF NOT EXISTS idx_systemhealthchecks_name ON SystemHealthChecks(CheckName);
CREATE INDEX IF NOT EXISTS idx_systemhealthchecks_type ON SystemHealthChecks(CheckType);
CREATE INDEX IF NOT EXISTS idx_systemhealthchecks_status ON SystemHealthChecks(Status);
CREATE INDEX IF NOT EXISTS idx_systemhealthchecks_checked ON SystemHealthChecks(LastChecked);

-- System Metrics Indexes
CREATE INDEX IF NOT EXISTS idx_systemmetrics_name ON SystemMetrics(MetricName);
CREATE INDEX IF NOT EXISTS idx_systemmetrics_type ON SystemMetrics(MetricType);
CREATE INDEX IF NOT EXISTS idx_systemmetrics_recorded ON SystemMetrics(RecordedAt);

-- Api Usage Analytics Indexes
CREATE INDEX IF NOT EXISTS idx_apiusage_endpoint ON ApiUsageAnalytics(Endpoint);
CREATE INDEX IF NOT EXISTS idx_apiusage_tenant ON ApiUsageAnalytics(TenantId);
CREATE INDEX IF NOT EXISTS idx_apiusage_user ON ApiUsageAnalytics(UserId);
CREATE INDEX IF NOT EXISTS idx_apiusage_date ON ApiUsageAnalytics(DateRecorded);

-- Backup Jobs Indexes
CREATE INDEX IF NOT EXISTS idx_backupjobs_type ON BackupJobs(BackupType);
CREATE INDEX IF NOT EXISTS idx_backupjobs_scope ON BackupJobs(BackupScope);
CREATE INDEX IF NOT EXISTS idx_backupjobs_active ON BackupJobs(IsActive);
CREATE INDEX IF NOT EXISTS idx_backupjobs_nextrun ON BackupJobs(NextRunAt);

-- Backup Executions Indexes
CREATE INDEX IF NOT EXISTS idx_backupexecutions_job ON BackupExecutions(BackupJobId);
CREATE INDEX IF NOT EXISTS idx_backupexecutions_id ON BackupExecutions(ExecutionId);
CREATE INDEX IF NOT EXISTS idx_backupexecutions_status ON BackupExecutions(Status);
CREATE INDEX IF NOT EXISTS idx_backupexecutions_started ON BackupExecutions(StartedAt);

-- Compliance Reports Indexes
CREATE INDEX IF NOT EXISTS idx_compliancereports_type ON ComplianceReports(ComplianceType);
CREATE INDEX IF NOT EXISTS idx_compliancereports_period ON ComplianceReports(ReportPeriod);
CREATE INDEX IF NOT EXISTS idx_compliancereports_status ON ComplianceReports(Status);
CREATE INDEX IF NOT EXISTS idx_compliancereports_risk ON ComplianceReports(RiskLevel);

-- Data Retention Policies Indexes
CREATE INDEX IF NOT EXISTS idx_dataretention_table ON DataRetentionPolicies(TableName);
CREATE INDEX IF NOT EXISTS idx_dataretention_active ON DataRetentionPolicies(IsActive);
CREATE INDEX IF NOT EXISTS idx_dataretention_nextexecution ON DataRetentionPolicies(NextExecution);

-- ========================================
-- Triggers for Super Admin Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to Super Admin tables
CREATE TRIGGER update_systemconfig_updated_at BEFORE UPDATE ON SystemConfiguration 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_systemmaintenance_updated_at BEFORE UPDATE ON SystemMaintenance 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tenants_updated_at BEFORE UPDATE ON Tenants 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tenantsettings_updated_at BEFORE UPDATE ON TenantSettings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON Users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_backupjobs_updated_at BEFORE UPDATE ON BackupJobs 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_dataretentionpolicies_updated_at BEFORE UPDATE ON DataRetentionPolicies 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to log user activity
CREATE OR REPLACE FUNCTION log_user_activity()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO UserActivityLogs (UserId, TenantId, ActivityType, ResourceType, ResourceId, ResourceName, Action, Details)
        VALUES (NEW.Id, NEW.TenantId, 'Create', TG_TABLE_NAME, NEW.Id, NULL, 'User Created', 
                'New user account created: ' || NEW.Username);
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.IsActive IS DISTINCT FROM NEW.IsActive THEN
            INSERT INTO UserActivityLogs (UserId, TenantId, ActivityType, ResourceType, ResourceId, ResourceName, Action, Details)
            VALUES (NEW.Id, NEW.TenantId, 'Update', TG_TABLE_NAME, NEW.Id, NEW.Username, 
                    CASE WHEN NEW.IsActive THEN 'User Activated' ELSE 'User Deactivated' END,
                    'User status changed from ' || CASE WHEN OLD.IsActive THEN 'Active' ELSE 'Inactive' END || 
                    ' to ' || CASE WHEN NEW.IsActive THEN 'Active' ELSE 'Inactive' END);
        END IF;
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER user_activity_trigger
    AFTER INSERT OR UPDATE ON Users
    FOR EACH ROW EXECUTE FUNCTION log_user_activity();

-- ========================================
-- Views for Super Admin Dashboard
-- ========================================

-- View for System Overview
CREATE OR REPLACE VIEW SystemOverview AS
SELECT 
    (SELECT COUNT(*) FROM Tenants WHERE IsActive = true) as ActiveTenants,
    (SELECT COUNT(*) FROM Tenants WHERE IsActive = false) as InactiveTenants,
    (SELECT COUNT(*) FROM Users WHERE IsActive = true) as ActiveUsers,
    (SELECT COUNT(*) FROM Users WHERE IsActive = false) as InactiveUsers,
    (SELECT COUNT(*) FROM SystemLogs WHERE CreatedAt >= CURRENT_DATE) as TodayLogEntries,
    (SELECT COUNT(*) FROM SystemLogs WHERE LogLevel = 'ERROR' AND CreatedAt >= CURRENT_DATE) as TodayErrors,
    (SELECT COUNT(*) FROM BackupExecutions WHERE Status = 'Completed' AND DATE(CompletedAt) = CURRENT_DATE) as SuccessfulBackups,
    (SELECT COUNT(*) FROM SystemHealthChecks WHERE Status != 'Healthy') as UnhealthyChecks;

-- View for Tenant Statistics
CREATE OR REPLACE VIEW TenantStatistics AS
SELECT 
    t.Id,
    t.BusinessName,
    t.SubscriptionPlanId,
    sp.Name as SubscriptionPlan,
    t.MaxUsers,
    COUNT(DISTINCT u.Id) as CurrentUsers,
    t.MaxProducts,
    COUNT(DISTINCT p.Id) as CurrentProducts,
    COALESCE(tus.TotalRevenue, 0) as MonthlyRevenue,
    t.IsActive,
    t.IsApproved,
    t.CreatedAt
FROM Tenants t
LEFT JOIN SubscriptionPlans sp ON t.SubscriptionPlanId = sp.Id
LEFT JOIN Users u ON t.Id = u.TenantId AND u.IsActive = true
LEFT JOIN Products p ON t.Id = p.TenantId AND p.IsActive = true
LEFT JOIN TenantUsageStatistics tus ON t.Id = tus.TenantId 
    AND tus.StatisticsDate = DATE(CURRENT_DATE - INTERVAL '1 day')
GROUP BY t.Id, t.BusinessName, t.SubscriptionPlanId, sp.Name, t.MaxUsers, t.MaxProducts, 
         tus.TotalRevenue, t.IsActive, t.IsApproved, t.CreatedAt;

-- View for System Health Summary
CREATE OR REPLACE VIEW SystemHealthSummary AS
SELECT 
    CheckName,
    CheckType,
    Status,
    ResponseTime,
    LastChecked,
    CASE 
        WHEN Status = 'Healthy' THEN 'success'
        WHEN Status = 'Warning' THEN 'warning'
        WHEN Status = 'Critical' THEN 'danger'
        ELSE 'secondary'
    END as StatusClass
FROM SystemHealthChecks
ORDER BY 
    CASE 
        WHEN Status = 'Critical' THEN 1
        WHEN Status = 'Warning' THEN 2
        WHEN Status = 'Unknown' THEN 3
        WHEN Status = 'Healthy' THEN 4
    END,
    CheckName;

-- View for Recent System Activity
CREATE OR REPLACE VIEW RecentSystemActivity AS
SELECT 
    sl.Id,
    sl.LogLevel,
    sl.Message,
    sl.Source,
    u.FirstName || ' ' || u.LastName as UserName,
    t.BusinessName as TenantName,
    sl.CreatedAt
FROM SystemLogs sl
LEFT JOIN Users u ON sl.UserId = u.Id
LEFT JOIN Tenants t ON sl.TenantId = t.Id
ORDER BY sl.CreatedAt DESC
LIMIT 50;
