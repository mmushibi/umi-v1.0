-- Authentication and Account Flow Tables
-- This file contains SQL for user authentication, signup, signin, and account management

-- ========================================
-- User Authentication Tables
-- ========================================

-- User Accounts Table (extended for comprehensive account management)
CREATE TABLE IF NOT EXISTS UserAccounts (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Salt VARCHAR(100) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    MiddleName VARCHAR(100),
    DisplayName VARCHAR(200),
    ProfilePicture VARCHAR(500),
    DateOfBirth DATE,
    Gender VARCHAR(10) CHECK (Gender IN ('Male', 'Female', 'Other', 'Prefer not to say')),
    PhoneNumber VARCHAR(20),
    AlternativePhoneNumber VARCHAR(20),
    NationalIdNumber VARCHAR(50),
    PassportNumber VARCHAR(50),
    Address TEXT,
    City VARCHAR(100),
    Province VARCHAR(100),
    Country VARCHAR(100) DEFAULT 'Zambia',
    PostalCode VARCHAR(20),
    
    -- Employment Information
    EmployeeId VARCHAR(50) UNIQUE,
    JobTitle VARCHAR(100),
    Department VARCHAR(100),
    HireDate DATE,
    EmploymentStatus VARCHAR(20) DEFAULT 'Active' CHECK (EmploymentStatus IN ('Active', 'On Leave', 'Terminated', 'Retired')),
    
    -- Access Control
    Role VARCHAR(50) NOT NULL CHECK (Role IN ('SuperAdmin', 'OperationsAdmin', 'SalesTeamAdmin', 'TenantAdmin', 'BranchManager', 'Pharmacist', 'Cashier', 'SalesRep', 'Accountant')),
    Permissions TEXT, -- JSON array of specific permissions
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    
    -- Authentication Status
    IsActive BOOLEAN DEFAULT true,
    IsEmailVerified BOOLEAN DEFAULT false,
    IsPhoneVerified BOOLEAN DEFAULT false,
    IsLocked BOOLEAN DEFAULT false,
    LockoutReason VARCHAR(200),
    LockoutUntil TIMESTAMP WITH TIME ZONE,
    
    -- Security Tracking
    FailedLoginAttempts INTEGER DEFAULT 0,
    LastSuccessfulLogin TIMESTAMP WITH TIME ZONE,
    LastFailedLogin TIMESTAMP WITH TIME ZONE,
    LastPasswordChange TIMESTAMP WITH TIME ZONE,
    PasswordChangeRequired BOOLEAN DEFAULT false,
    TwoFactorEnabled BOOLEAN DEFAULT false,
    TwoFactorSecret VARCHAR(255),
    BackupCodes TEXT, -- JSON array of backup codes
    
    -- Session Management
    CurrentSessionId VARCHAR(255),
    CurrentSessionStart TIMESTAMP WITH TIME ZONE,
    CurrentSessionIpAddress VARCHAR(45),
    CurrentSessionUserAgent TEXT,
    
    -- Audit Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id)
);

-- User Verification Tokens Table (for email, phone verification)
CREATE TABLE IF NOT EXISTS UserVerificationTokens (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    TokenType VARCHAR(20) NOT NULL CHECK (TokenType IN ('EmailVerification', 'PhoneVerification', 'PasswordReset', 'AccountActivation')),
    Token VARCHAR(255) NOT NULL UNIQUE,
    TokenHash VARCHAR(255) NOT NULL,
    ExpiryDate TIMESTAMP WITH TIME ZONE NOT NULL,
    IsUsed BOOLEAN DEFAULT false,
    UsedAt TIMESTAMP WITH TIME ZONE,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Sessions Table (detailed session tracking)
CREATE TABLE IF NOT EXISTS UserSessions (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    SessionId VARCHAR(255) UNIQUE NOT NULL,
    SessionToken VARCHAR(255) NOT NULL,
    RefreshToken VARCHAR(255),
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    DeviceType VARCHAR(50),
    Browser VARCHAR(100),
    OperatingSystem VARCHAR(100),
    Location VARCHAR(200),
    LoginTime TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    LastActivity TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    LogoutTime TIMESTAMP WITH TIME ZONE,
    LogoutReason VARCHAR(50), -- 'User', 'Timeout', 'Admin', 'Security'
    IsActive BOOLEAN DEFAULT true,
    ExpiresAt TIMESTAMP WITH TIME ZONE NOT NULL
);

-- User Security Logs Table (detailed security event tracking)
CREATE TABLE IF NOT EXISTS UserSecurityLogs (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    EventType VARCHAR(50) NOT NULL CHECK (EventType IN ('Login', 'Logout', 'FailedLogin', 'PasswordChange', 'AccountLocked', 'AccountUnlocked', 'PasswordReset', 'TwoFactorEnabled', 'TwoFactorDisabled', 'PermissionGranted', 'PermissionRevoked')),
    EventDescription TEXT,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    Location VARCHAR(200),
    Success BOOLEAN NOT NULL,
    FailureReason VARCHAR(200),
    RiskScore INTEGER DEFAULT 0, -- 0-100 risk assessment
    AdditionalData TEXT, -- JSON with additional event details
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Account Management Tables
-- ========================================

-- User Preferences Table (personalized settings)
CREATE TABLE IF NOT EXISTS UserPreferences (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    Category VARCHAR(50) NOT NULL, -- 'Dashboard', 'UI', 'Notifications', 'Reports', 'POS'
    PreferenceKey VARCHAR(100) NOT NULL,
    PreferenceValue TEXT,
    DataType VARCHAR(20) DEFAULT 'String' CHECK (DataType IN ('String', 'Integer', 'Boolean', 'JSON')),
    IsSystem BOOLEAN DEFAULT false, -- system vs user preference
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(UserId, Category, PreferenceKey)
);

-- User Notifications Table (in-app notifications)
CREATE TABLE IF NOT EXISTS UserNotifications (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    Title VARCHAR(200) NOT NULL,
    Message TEXT NOT NULL,
    NotificationType VARCHAR(30) NOT NULL CHECK (NotificationType IN ('Info', 'Success', 'Warning', 'Error', 'System', 'Security', 'Reminder')),
    Category VARCHAR(50), -- 'Sales', 'Inventory', 'Prescription', 'System', 'Security'
    Priority VARCHAR(20) DEFAULT 'Normal' CHECK (Priority IN ('Low', 'Normal', 'High', 'Urgent')),
    ActionUrl VARCHAR(500),
    ActionText VARCHAR(100),
    IsRead BOOLEAN DEFAULT false,
    ReadAt TIMESTAMP WITH TIME ZONE,
    IsEmailSent BOOLEAN DEFAULT false,
    EmailSentAt TIMESTAMP WITH TIME ZONE,
    IsSmsSent BOOLEAN DEFAULT false,
    SmsSentAt TIMESTAMP WITH TIME ZONE,
    ExpiresAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Activity Timeline Table (comprehensive user activity tracking)
CREATE TABLE IF NOT EXISTS UserActivityTimeline (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    ActivityType VARCHAR(50) NOT NULL, -- 'Login', 'Logout', 'Sale', 'Prescription', 'InventoryUpdate', 'ReportGenerated', etc.
    ActivityDescription TEXT,
    Module VARCHAR(50), -- 'POS', 'Inventory', 'Patients', 'Prescriptions', 'Reports', etc.
    Action VARCHAR(100), -- 'Create', 'Read', 'Update', 'Delete', 'Login', 'Logout', etc.
    ResourceType VARCHAR(50), -- 'Sale', 'Patient', 'Prescription', 'Product', etc.
    ResourceId INTEGER,
    ResourceName VARCHAR(200),
    OldValues TEXT, -- JSON with previous values
    NewValues TEXT, -- JSON with new values
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    SessionId VARCHAR(255),
    Duration INTEGER, -- in seconds for applicable activities
    Success BOOLEAN DEFAULT true,
    ErrorMessage VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Password Management Tables
-- ========================================

-- Password History Table (prevent password reuse)
CREATE TABLE IF NOT EXISTS PasswordHistory (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    PasswordHash VARCHAR(255) NOT NULL,
    Salt VARCHAR(100) NOT NULL,
    ChangedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ChangedBy INTEGER REFERENCES UserAccounts(Id),
    ChangeReason VARCHAR(100), -- 'Initial', 'User', 'Admin', 'Expired', 'Security'
    IpAddress VARCHAR(45),
    UserAgent TEXT
);

-- Password Reset Requests Table (track password reset requests)
CREATE TABLE IF NOT EXISTS PasswordResetRequests (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    ResetToken VARCHAR(255) UNIQUE NOT NULL,
    TokenHash VARCHAR(255) NOT NULL,
    RequestedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt TIMESTAMP WITH TIME ZONE NOT NULL,
    IsUsed BOOLEAN DEFAULT false,
    UsedAt TIMESTAMP WITH TIME ZONE,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    EmailSent BOOLEAN DEFAULT false,
    EmailSentAt TIMESTAMP WITH TIME ZONE
);

-- ========================================
-- Two-Factor Authentication Tables
-- ========================================

-- TwoFactorAuthentication Table (2FA configuration)
CREATE TABLE IF NOT EXISTS TwoFactorAuthentication (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    TwoFactorMethod VARCHAR(20) NOT NULL CHECK (TwoFactorMethod IN ('TOTP', 'SMS', 'Email', 'HardwareToken')),
    Secret VARCHAR(255),
    BackupCodes TEXT, -- JSON array of backup codes
    PhoneNumber VARCHAR(20), -- for SMS 2FA
    EmailAddress VARCHAR(100), -- for email 2FA
    IsEnabled BOOLEAN DEFAULT false,
    EnabledAt TIMESTAMP WITH TIME ZONE,
    LastUsedAt TIMESTAMP WITH TIME ZONE,
    FailedAttempts INTEGER DEFAULT 0,
    IsLocked BOOLEAN DEFAULT false,
    LockedUntil TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- TwoFactorAuthenticationLogs Table (2FA usage tracking)
CREATE TABLE IF NOT EXISTS TwoFactorAuthenticationLogs (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    TwoFactorAuthId INTEGER REFERENCES TwoFactorAuthentication(Id) ON DELETE SET NULL,
    Action VARCHAR(20) NOT NULL CHECK (Action IN ('Setup', 'Verify', 'Disable', 'BackupCodeUsed', 'FailedAttempt')),
    Code VARCHAR(10), -- last 4 digits for logging
    Success BOOLEAN NOT NULL,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- User Roles and Permissions Tables
-- ========================================

-- Roles Table (dynamic role management)
CREATE TABLE IF NOT EXISTS Roles (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    DisplayName VARCHAR(100) NOT NULL,
    Description TEXT,
    RoleType VARCHAR(20) DEFAULT 'Custom' CHECK (RoleType IN ('System', 'Custom')),
    IsSystem BOOLEAN DEFAULT false,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Permissions Table (granular permissions)
CREATE TABLE IF NOT EXISTS Permissions (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) UNIQUE NOT NULL,
    DisplayName VARCHAR(200) NOT NULL,
    Description TEXT,
    Module VARCHAR(50) NOT NULL, -- 'POS', 'Inventory', 'Patients', 'Prescriptions', 'Reports', etc.
    Action VARCHAR(50) NOT NULL, -- 'Create', 'Read', 'Update', 'Delete', 'Approve', 'Export', etc.
    Resource VARCHAR(50), -- 'Sale', 'Patient', 'Prescription', 'Product', etc.
    IsSystem BOOLEAN DEFAULT false,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- RolePermissions Table (mapping roles to permissions)
CREATE TABLE IF NOT EXISTS RolePermissions (
    Id SERIAL PRIMARY KEY,
    RoleId INTEGER NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
    PermissionId INTEGER NOT NULL REFERENCES Permissions(Id) ON DELETE CASCADE,
    IsGranted BOOLEAN DEFAULT true,
    GrantedBy INTEGER REFERENCES UserAccounts(Id),
    GrantedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(RoleId, PermissionId)
);

-- UserRoles Table (assigning roles to users)
CREATE TABLE IF NOT EXISTS UserRoles (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    RoleId INTEGER NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    AssignedBy INTEGER REFERENCES UserAccounts(Id),
    AssignedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt TIMESTAMP WITH TIME ZONE,
    IsActive BOOLEAN DEFAULT true,
    UNIQUE(UserId, RoleId, TenantId, BranchId)
);

-- UserSpecificPermissions Table (direct user permissions)
CREATE TABLE IF NOT EXISTS UserSpecificPermissions (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    PermissionId INTEGER NOT NULL REFERENCES Permissions(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    IsGranted BOOLEAN DEFAULT true,
    GrantedBy INTEGER REFERENCES UserAccounts(Id),
    GrantedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ExpiresAt TIMESTAMP WITH TIME ZONE,
    Reason VARCHAR(500),
    UNIQUE(UserId, PermissionId, TenantId, BranchId)
);

-- ========================================
-- Account Registration Tables
-- ========================================

-- AccountRegistrationRequests Table (for new account requests)
CREATE TABLE IF NOT EXISTS AccountRegistrationRequests (
    Id SERIAL PRIMARY KEY,
    RequestId VARCHAR(50) UNIQUE NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PhoneNumber VARCHAR(20),
    BusinessName VARCHAR(200),
    BusinessType VARCHAR(50),
    RequestedRole VARCHAR(50),
    Reason VARCHAR(1000),
    Status VARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'RequiresMoreInfo')),
    ReviewedBy INTEGER REFERENCES UserAccounts(Id),
    ReviewedAt TIMESTAMP WITH TIME ZONE,
    ReviewComments TEXT,
    ApprovalToken VARCHAR(255),
    ApprovalTokenExpires TIMESTAMP WITH TIME ZONE,
    RegisteredAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IpAddress VARCHAR(45),
    UserAgent TEXT
);

-- AccountInvitations Table (for user invitations)
CREATE TABLE IF NOT EXISTS AccountInvitations (
    Id SERIAL PRIMARY KEY,
    InvitationToken VARCHAR(255) UNIQUE NOT NULL,
    Email VARCHAR(100) NOT NULL,
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    Role VARCHAR(50),
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    InvitedBy INTEGER NOT NULL REFERENCES UserAccounts(Id),
    Message TEXT,
    Status VARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Accepted', 'Declined', 'Expired')),
    SentAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    AcceptedAt TIMESTAMP WITH TIME ZONE,
    DeclinedAt TIMESTAMP WITH TIME ZONE,
    ExpiresAt TIMESTAMP WITH TIME ZONE NOT NULL
);

-- ========================================
-- Indexes for Authentication Tables
-- ========================================

-- UserAccounts Indexes
CREATE INDEX IF NOT EXISTS idx_useraccounts_username ON UserAccounts(Username);
CREATE INDEX IF NOT EXISTS idx_useraccounts_email ON UserAccounts(Email);
CREATE INDEX IF NOT EXISTS idx_useraccounts_employeeid ON UserAccounts(EmployeeId);
CREATE INDEX IF NOT EXISTS idx_useraccounts_role ON UserAccounts(Role);
CREATE INDEX IF NOT EXISTS idx_useraccounts_tenant ON UserAccounts(TenantId);
CREATE INDEX IF NOT EXISTS idx_useraccounts_branch ON UserAccounts(BranchId);
CREATE INDEX IF NOT EXISTS idx_useraccounts_active ON UserAccounts(IsActive);
CREATE INDEX IF NOT EXISTS idx_useraccounts_locked ON UserAccounts(IsLocked);
CREATE INDEX IF NOT EXISTS idx_useraccounts_verified ON UserAccounts(IsEmailVerified);
CREATE INDEX IF NOT EXISTS idx_useraccounts_created ON UserAccounts(CreatedAt);

-- UserVerificationTokens Indexes
CREATE INDEX IF NOT EXISTS idx_userverificationtokens_user ON UserVerificationTokens(UserId);
CREATE INDEX IF NOT EXISTS idx_userverificationtokens_token ON UserVerificationTokens(Token);
CREATE INDEX IF NOT EXISTS idx_userverificationtokens_type ON UserVerificationTokens(TokenType);
CREATE INDEX IF NOT EXISTS idx_userverificationtokens_expiry ON UserVerificationTokens(ExpiryDate);

-- UserSessions Indexes
CREATE INDEX IF NOT EXISTS idx_usersessions_user ON UserSessions(UserId);
CREATE INDEX IF NOT EXISTS idx_usersessions_sessionid ON UserSessions(SessionId);
CREATE INDEX IF NOT EXISTS idx_usersessions_token ON UserSessions(SessionToken);
CREATE INDEX IF NOT EXISTS idx_usersessions_ip ON UserSessions(IpAddress);
CREATE INDEX IF NOT EXISTS idx_usersessions_active ON UserSessions(IsActive);
CREATE INDEX IF NOT EXISTS idx_usersessions_expires ON UserSessions(ExpiresAt);

-- UserSecurityLogs Indexes
CREATE INDEX IF NOT EXISTS idx_usersecuritylogs_user ON UserSecurityLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_usersecuritylogs_type ON UserSecurityLogs(EventType);
CREATE INDEX IF NOT EXISTS idx_usersecuritylogs_success ON UserSecurityLogs(Success);
CREATE INDEX IF NOT EXISTS idx_usersecuritylogs_risk ON UserSecurityLogs(RiskScore);
CREATE INDEX IF NOT EXISTS idx_usersecuritylogs_created ON UserSecurityLogs(CreatedAt);

-- UserPreferences Indexes
CREATE INDEX IF NOT EXISTS idx_userpreferences_user ON UserPreferences(UserId);
CREATE INDEX IF NOT EXISTS idx_userpreferences_category ON UserPreferences(Category);

-- UserNotifications Indexes
CREATE INDEX IF NOT EXISTS idx_usernotifications_user ON UserNotifications(UserId);
CREATE INDEX IF NOT EXISTS idx_usernotifications_tenant ON UserNotifications(TenantId);
CREATE INDEX IF NOT EXISTS idx_usernotifications_type ON UserNotifications(NotificationType);
CREATE INDEX IF NOT EXISTS idx_usernotifications_read ON UserNotifications(IsRead);
CREATE INDEX IF NOT EXISTS idx_usernotifications_priority ON UserNotifications(Priority);
CREATE INDEX IF NOT EXISTS idx_usernotifications_created ON UserNotifications(CreatedAt);

-- UserActivityTimeline Indexes
CREATE INDEX IF NOT EXISTS idx_useractivitytimeline_user ON UserActivityTimeline(UserId);
CREATE INDEX IF NOT EXISTS idx_useractivitytimeline_tenant ON UserActivityTimeline(TenantId);
CREATE INDEX IF NOT EXISTS idx_useractivitytimeline_type ON UserActivityTimeline(ActivityType);
CREATE INDEX IF NOT EXISTS idx_useractivitytimeline_module ON UserActivityTimeline(Module);
CREATE INDEX IF NOT EXISTS idx_useractivitytimeline_resource ON UserActivityTimeline(ResourceType, ResourceId);
CREATE INDEX IF NOT EXISTS idx_useractivitytimeline_created ON UserActivityTimeline(CreatedAt);

-- Password History Indexes
CREATE INDEX IF NOT EXISTS idx_passwordhistory_user ON PasswordHistory(UserId);
CREATE INDEX IF NOT EXISTS idx_passwordhistory_changed ON PasswordHistory(ChangedAt);

-- Password Reset Requests Indexes
CREATE INDEX IF NOT EXISTS idx_passwordresetrequests_user ON PasswordResetRequests(UserId);
CREATE INDEX IF NOT EXISTS idx_passwordresetrequests_token ON PasswordResetRequests(ResetToken);
CREATE INDEX IF NOT EXISTS idx_passwordresetrequests_expires ON PasswordResetRequests(ExpiresAt);

-- TwoFactorAuthentication Indexes
CREATE INDEX IF NOT EXISTS idx_twofactorauth_user ON TwoFactorAuthentication(UserId);
CREATE INDEX IF NOT EXISTS idx_twofactorauth_enabled ON TwoFactorAuthentication(IsEnabled);

-- TwoFactorAuthenticationLogs Indexes
CREATE INDEX IF NOT EXISTS idx_twofactorauthlogs_user ON TwoFactorAuthenticationLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_twofactorauthlogs_action ON TwoFactorAuthenticationLogs(Action);

-- Roles and Permissions Indexes
CREATE INDEX IF NOT EXISTS idx_roles_name ON Roles(Name);
CREATE INDEX IF NOT EXISTS idx_roles_active ON Roles(IsActive);
CREATE INDEX IF NOT EXISTS idx_permissions_module ON Permissions(Module);
CREATE INDEX IF NOT EXISTS idx_permissions_action ON Permissions(Action);
CREATE INDEX IF NOT EXISTS idx_rolepermissions_role ON RolePermissions(RoleId);
CREATE INDEX IF NOT EXISTS idx_rolepermissions_permission ON RolePermissions(PermissionId);
CREATE INDEX IF NOT EXISTS idx_userroles_user ON UserRoles(UserId);
CREATE INDEX IF NOT EXISTS idx_userroles_role ON UserRoles(RoleId);
CREATE INDEX IF NOT EXISTS idx_userroles_tenant ON UserRoles(TenantId);
CREATE INDEX IF NOT EXISTS idx_userroles_active ON UserRoles(IsActive);

-- Account Registration Indexes
CREATE INDEX IF NOT EXISTS idx_accountregistrationrequests_email ON AccountRegistrationRequests(Email);
CREATE INDEX IF NOT EXISTS idx_accountregistrationrequests_status ON AccountRegistrationRequests(Status);
CREATE INDEX IF NOT EXISTS idx_accountregistrationrequests_created ON AccountRegistrationRequests(RegisteredAt);

-- Account Invitations Indexes
CREATE INDEX IF NOT EXISTS idx_accountinvitations_token ON AccountInvitations(InvitationToken);
CREATE INDEX IF NOT EXISTS idx_accountinvitations_email ON AccountInvitations(Email);
CREATE INDEX IF NOT EXISTS idx_accountinvitations_tenant ON AccountInvitations(TenantId);
CREATE INDEX IF NOT EXISTS idx_accountinvitations_status ON AccountInvitations(Status);
CREATE INDEX IF NOT EXISTS idx_accountinvitations_expires ON AccountInvitations(ExpiresAt);

-- ========================================
-- Triggers for Authentication Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to authentication tables
CREATE TRIGGER update_useraccounts_updated_at BEFORE UPDATE ON UserAccounts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_userpreferences_updated_at BEFORE UPDATE ON UserPreferences 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_twofactorauth_updated_at BEFORE UPDATE ON TwoFactorAuthentication 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON Roles 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to log user activity
CREATE OR REPLACE FUNCTION log_user_activity()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO UserActivityTimeline (UserId, ActivityType, ActivityDescription, Module, Action, ResourceType, ResourceId, ResourceName, NewValues)
        VALUES (NEW.Id, 'Account Created', 'User account created', 'Authentication', 'Create', 'UserAccount', NEW.Id, NEW.Username, row_to_json(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.IsActive IS DISTINCT FROM NEW.IsActive THEN
            INSERT INTO UserActivityTimeline (UserId, ActivityType, ActivityDescription, Module, Action, ResourceType, ResourceId, ResourceName, OldValues, NewValues)
            VALUES (NEW.Id, 'Account Status Changed', 'User account status changed', 'Authentication', 'Update', 'UserAccount', NEW.Id, NEW.Username, 
                    row_to_json(OLD), row_to_json(NEW));
        END IF;
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER user_activity_trigger
    AFTER INSERT OR UPDATE ON UserAccounts
    FOR EACH ROW EXECUTE FUNCTION log_user_activity();

-- Function to automatically clean up expired sessions
CREATE OR REPLACE FUNCTION cleanup_expired_sessions()
RETURNS void AS $$
BEGIN
    UPDATE UserSessions 
    SET IsActive = false, LogoutTime = CURRENT_TIMESTAMP, LogoutReason = 'Timeout'
    WHERE IsActive = true AND ExpiresAt < CURRENT_TIMESTAMP;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- Views for Authentication Management
-- ========================================

-- View for User Account Summary
CREATE OR REPLACE VIEW UserAccountSummary AS
SELECT 
    ua.Id,
    ua.Username,
    ua.Email,
    ua.FirstName,
    ua.LastName,
    ua.DisplayName,
    ua.Role,
    ua.IsActive,
    ua.IsEmailVerified,
    ua.IsLocked,
    ua.LastSuccessfulLogin,
    ua.FailedLoginAttempts,
    ua.TenantId,
    t.BusinessName as TenantName,
    ua.BranchId,
    b.Name as BranchName,
    ua.CreatedAt,
    CASE 
        WHEN ua.IsLocked THEN 'Locked'
        WHEN NOT ua.IsActive THEN 'Inactive'
        WHEN NOT ua.IsEmailVerified THEN 'Unverified'
        ELSE 'Active'
    END as AccountStatus
FROM UserAccounts ua
LEFT JOIN Tenants t ON ua.TenantId = t.Id
LEFT JOIN Branches b ON ua.BranchId = b.Id;

-- View for Active Sessions
CREATE OR REPLACE VIEW ActiveUserSessions AS
SELECT 
    us.Id,
    us.UserId,
    ua.Username,
    ua.FirstName || ' ' || ua.LastName as UserName,
    us.SessionId,
    us.IpAddress,
    us.DeviceType,
    us.Browser,
    us.Location,
    us.LoginTime,
    us.LastActivity,
    us.ExpiresAt,
    EXTRACT(EPOCH FROM (us.ExpiresAt - CURRENT_TIMESTAMP)) / 60 as MinutesRemaining
FROM UserSessions us
JOIN UserAccounts ua ON us.UserId = ua.Id
WHERE us.IsActive = true
ORDER BY us.LastActivity DESC;

-- View for Security Events
CREATE OR REPLACE VIEW RecentSecurityEvents AS
SELECT 
    usl.Id,
    usl.UserId,
    ua.Username,
    ua.FirstName || ' ' || ua.LastName as UserName,
    usl.EventType,
    usl.EventDescription,
    usl.Success,
    usl.FailureReason,
    usl.RiskScore,
    usl.IpAddress,
    usl.Location,
    usl.CreatedAt
FROM UserSecurityLogs usl
LEFT JOIN UserAccounts ua ON usl.UserId = ua.Id
ORDER BY usl.CreatedAt DESC
LIMIT 100;

-- View for User Permissions
CREATE OR REPLACE VIEW UserEffectivePermissions AS
SELECT DISTINCT
    ua.Id as UserId,
    ua.Username,
    p.Name as PermissionName,
    p.DisplayName as PermissionDisplayName,
    p.Module,
    p.Action,
    p.Resource,
    COALESCE(ur.TenantId, ua.TenantId) as TenantId,
    COALESCE(ur.BranchId, ua.BranchId) as BranchId
FROM UserAccounts ua
LEFT JOIN UserRoles ur ON ua.Id = ur.UserId AND ur.IsActive = true
LEFT JOIN RolePermissions rp ON ur.RoleId = rp.RoleId AND rp.IsGranted = true
LEFT JOIN Permissions p ON rp.PermissionId = p.Id
LEFT JOIN UserSpecificPermissions usp ON ua.Id = usp.UserId AND usp.IsGranted = true
LEFT JOIN Permissions p2 ON usp.PermissionId = p2.Id
WHERE ua.IsActive = true
  AND (rp.PermissionId IS NOT NULL OR usp.PermissionId IS NOT NULL)
UNION
SELECT DISTINCT
    ua.Id as UserId,
    ua.Username,
    p2.Name as PermissionName,
    p2.DisplayName as PermissionDisplayName,
    p2.Module,
    p2.Action,
    p2.Resource,
    usp.TenantId,
    usp.BranchId
FROM UserAccounts ua
JOIN UserSpecificPermissions usp ON ua.Id = usp.UserId AND usp.IsGranted = true
JOIN Permissions p2 ON usp.PermissionId = p2.Id
WHERE ua.IsActive = true;
