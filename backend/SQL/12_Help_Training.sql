-- Help and Training Tables
-- This file contains SQL for help system, training management, knowledge base, and user support

-- ========================================
-- Help and Knowledge Base Tables
-- ========================================

-- HelpCategories Table (organizing help content)
CREATE TABLE IF NOT EXISTS HelpCategories (
    Id SERIAL PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL,
    CategoryCode VARCHAR(20) UNIQUE NOT NULL,
    ParentCategoryId INTEGER REFERENCES HelpCategories(Id) ON DELETE SET NULL,
    
    -- Category Details
    Description TEXT,
    Icon VARCHAR(50),
    Color VARCHAR(20),
    DisplayOrder INTEGER DEFAULT 0,
    
    -- Access Control
    UserRoleAccess TEXT, -- JSON array of roles that can access this category
    IsPublic BOOLEAN DEFAULT true,
    IsActive BOOLEAN DEFAULT true,
    
    -- System Fields
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- HelpArticles Table (knowledge base articles)
CREATE TABLE IF NOT EXISTS HelpArticles (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Slug VARCHAR(200) UNIQUE NOT NULL,
    Content TEXT NOT NULL,
    Summary VARCHAR(500),
    
    -- Classification
    CategoryId INTEGER REFERENCES HelpCategories(Id) ON DELETE SET NULL,
    ArticleType VARCHAR(30) DEFAULT 'General' CHECK (ArticleType IN ('General', 'Tutorial', 'FAQ', 'Troubleshooting', 'Policy', 'Procedure', 'Video')),
    DifficultyLevel VARCHAR(20) DEFAULT 'Beginner' CHECK (DifficultyLevel IN ('Beginner', 'Intermediate', 'Advanced')),
    
    -- Metadata
    Tags TEXT, -- JSON array of tags
    Keywords VARCHAR(500),
    ReadingTime INTEGER, -- estimated reading time in minutes
    VideoUrl VARCHAR(500),
    VideoDuration INTEGER, -- in seconds
    
    -- Status and Publishing
    Status VARCHAR(20) DEFAULT 'Draft' CHECK (Status IN ('Draft', 'Review', 'Published', 'Archived')),
    PublishedAt TIMESTAMP WITH TIME ZONE,
    Featured BOOLEAN DEFAULT false,
    IsPublic BOOLEAN DEFAULT true,
    
    -- Access Control
    UserRoleAccess TEXT, -- JSON array of roles
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Analytics
    ViewCount INTEGER DEFAULT 0,
    HelpfulCount INTEGER DEFAULT 0,
    NotHelpfulCount INTEGER DEFAULT 0,
    LastViewedAt TIMESTAMP WITH TIME ZONE,
    
    -- System Fields
    AuthorId INTEGER REFERENCES UserAccounts(Id),
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- HelpArticleAttachments Table (files attached to help articles)
CREATE TABLE IF NOT EXISTS HelpArticleAttachments (
    Id SERIAL PRIMARY KEY,
    ArticleId INTEGER NOT NULL REFERENCES HelpArticles(Id) ON DELETE CASCADE,
    FileName VARCHAR(255) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    FileType VARCHAR(50) NOT NULL,
    MimeType VARCHAR(100),
    
    -- File Details
    Description VARCHAR(500),
    DisplayOrder INTEGER DEFAULT 0,
    IsPublic BOOLEAN DEFAULT true,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Training Management Tables
-- ========================================

-- TrainingCourses Table (training courses and modules)
CREATE TABLE IF NOT EXISTS TrainingCourses (
    Id SERIAL PRIMARY KEY,
    CourseName VARCHAR(200) NOT NULL,
    CourseCode VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    
    -- Course Details
    CourseType VARCHAR(30) DEFAULT 'Online' CHECK (CourseType IN ('Online', 'Classroom', 'Blended', 'Video', 'Document')),
    Category VARCHAR(50),
    DifficultyLevel VARCHAR(20) DEFAULT 'Beginner' CHECK (DifficultyLevel IN ('Beginner', 'Intermediate', 'Advanced')),
    
    -- Duration and Schedule
    DurationMinutes INTEGER DEFAULT 0,
    EstimatedHours DECIMAL(5,2) DEFAULT 0.00,
    IsSelfPaced BOOLEAN DEFAULT true,
    StartDate DATE,
    EndDate DATE,
    RegistrationDeadline DATE,
    
    -- Content and Materials
    Content TEXT, -- JSON with course structure
    LearningObjectives TEXT,
    Prerequisites TEXT,
    Materials TEXT, -- JSON with materials list
    
    -- Enrollment and Capacity
    MaxParticipants INTEGER,
    MinParticipants INTEGER DEFAULT 1,
    CurrentEnrollment INTEGER DEFAULT 0,
    WaitlistEnabled BOOLEAN DEFAULT false,
    
    -- Certification
    ProvidesCertificate BOOLEAN DEFAULT false,
    CertificateTemplate VARCHAR(200),
    PassingScore INTEGER DEFAULT 70,
    
    -- Status and Access
    Status VARCHAR(20) DEFAULT 'Draft' CHECK (Status IN ('Draft', 'Published', 'Archived', 'Cancelled')),
    IsActive BOOLEAN DEFAULT true,
    IsPublic BOOLEAN DEFAULT true,
    IsMandatory BOOLEAN DEFAULT false,
    
    -- Target Audience
    TargetRoles TEXT, -- JSON array of target roles
    TargetBranches TEXT, -- JSON array of target branches
    
    -- System Fields
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    InstructorId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- TrainingEnrollments Table (user enrollments in training courses)
CREATE TABLE IF NOT EXISTS TrainingEnrollments (
    Id SERIAL PRIMARY KEY,
    CourseId INTEGER NOT NULL REFERENCES TrainingCourses(Id) ON DELETE CASCADE,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Enrollment Details
    EnrollmentDate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    EnrollmentStatus VARCHAR(20) DEFAULT 'Enrolled' CHECK (EnrollmentStatus IN ('Enrolled', 'In Progress', 'Completed', 'Failed', 'Dropped', 'Waitlisted', 'Cancelled')),
    CompletionDate TIMESTAMP WITH TIME ZONE,
    
    -- Progress Tracking
    ProgressPercentage INTEGER DEFAULT 0,
    TimeSpentMinutes INTEGER DEFAULT 0,
    LastAccessedAt TIMESTAMP WITH TIME ZONE,
    CurrentModule INTEGER, -- ID of current module being studied
    
    -- Assessment Results
    Score INTEGER,
    MaxScore INTEGER,
    Grade VARCHAR(10),
    Passed BOOLEAN DEFAULT false,
    CertificateIssued BOOLEAN DEFAULT false,
    CertificateUrl VARCHAR(500),
    
    -- Feedback and Evaluation
    CourseRating INTEGER, -- 1-5 scale
    CourseFeedback TEXT,
    InstructorRating INTEGER, -- 1-5 scale
    InstructorFeedback TEXT,
    
    -- System Fields
    EnrolledBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(CourseId, UserId)
);

-- TrainingModules Table (course modules and lessons)
CREATE TABLE IF NOT EXISTS TrainingModules (
    Id SERIAL PRIMARY KEY,
    CourseId INTEGER NOT NULL REFERENCES TrainingCourses(Id) ON DELETE CASCADE,
    ModuleName VARCHAR(200) NOT NULL,
    ModuleTitle VARCHAR(200),
    ModuleDescription TEXT,
    
    -- Module Structure
    ModuleType VARCHAR(30) DEFAULT 'Content' CHECK (ModuleType IN ('Content', 'Video', 'Quiz', 'Assignment', 'Survey', 'Resource')),
    Content TEXT, -- JSON with module content
    VideoUrl VARCHAR(500),
    VideoDuration INTEGER, -- in seconds
    
    -- Ordering and Dependencies
    ModuleOrder INTEGER DEFAULT 0,
    ParentModuleId INTEGER REFERENCES TrainingModules(Id) ON DELETE SET NULL,
    PrerequisiteModuleIds TEXT, -- JSON array of prerequisite module IDs
    
    -- Assessment
    IsAssessable BOOLEAN DEFAULT false,
    PassingScore INTEGER DEFAULT 70,
    MaxAttempts INTEGER DEFAULT 3,
    TimeLimit INTEGER, -- in minutes
    
    -- Status
    IsActive BOOLEAN DEFAULT true,
    IsOptional BOOLEAN DEFAULT false,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- TrainingProgress Table (detailed progress tracking)
CREATE TABLE IF NOT EXISTS TrainingProgress (
    Id SERIAL PRIMARY KEY,
    EnrollmentId INTEGER NOT NULL REFERENCES TrainingEnrollments(Id) ON DELETE CASCADE,
    ModuleId INTEGER NOT NULL REFERENCES TrainingModules(Id) ON DELETE CASCADE,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    
    -- Progress Details
    Status VARCHAR(20) DEFAULT 'Not Started' CHECK (Status IN ('Not Started', 'In Progress', 'Completed', 'Failed', 'Skipped')),
    StartTime TIMESTAMP WITH TIME ZONE,
    CompletionTime TIMESTAMP WITH TIME ZONE,
    TimeSpentSeconds INTEGER DEFAULT 0,
    
    -- Assessment Results
    Score INTEGER,
    MaxScore INTEGER,
    Attempts INTEGER DEFAULT 0,
    Passed BOOLEAN DEFAULT false,
    
    -- Interaction Data
    LastPosition INTEGER, -- position in video or content
    BookmarkData TEXT, -- JSON with bookmark information
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(EnrollmentId, ModuleId)
);

-- ========================================
-- Support and Ticket Management Tables
-- ========================================

-- SupportTickets Table (customer support tickets)
CREATE TABLE IF NOT EXISTS SupportTickets (
    Id SERIAL PRIMARY KEY,
    TicketNumber VARCHAR(50) UNIQUE NOT NULL,
    Subject VARCHAR(200) NOT NULL,
    Description TEXT NOT NULL,
    
    -- Classification
    Category VARCHAR(50) NOT NULL CHECK (Category IN ('Technical', 'Account', 'Billing', 'Feature Request', 'Bug Report', 'Training', 'Other')),
    Priority VARCHAR(20) DEFAULT 'Normal' CHECK (Priority IN ('Low', 'Normal', 'High', 'Urgent', 'Critical')),
    Severity VARCHAR(20) DEFAULT 'Medium' CHECK (Severity IN ('Low', 'Medium', 'High', 'Critical')),
    
    -- User Information
    UserId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    CustomerName VARCHAR(200),
    CustomerEmail VARCHAR(100),
    CustomerPhone VARCHAR(20),
    
    -- Context Information
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE SET NULL,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    Module VARCHAR(50), -- module where issue occurred
    PageUrl VARCHAR(500),
    
    -- Status and Assignment
    Status VARCHAR(20) DEFAULT 'Open' CHECK (Status IN ('Open', 'In Progress', 'Pending Customer', 'Resolved', 'Closed', 'Reopened')),
    AssignedTo INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    AssignedAt TIMESTAMP WITH TIME ZONE,
    
    -- Resolution Information
    Resolution TEXT,
    ResolutionTime TIMESTAMP WITH TIME ZONE,
    ResolutionCategory VARCHAR(50), -- 'Bug Fix', 'User Error', 'Feature', 'Documentation', etc.
    
    -- Time Tracking
    FirstResponseTime TIMESTAMP WITH TIME ZONE,
    ResponseTimeMinutes INTEGER,
    ResolutionTimeMinutes INTEGER,
    
    -- Customer Satisfaction
    CustomerRating INTEGER, -- 1-5 scale
    CustomerFeedback TEXT,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- SupportTicketComments Table (ticket conversation and updates)
CREATE TABLE IF NOT EXISTS SupportTicketComments (
    Id SERIAL PRIMARY KEY,
    TicketId INTEGER NOT NULL REFERENCES SupportTickets(Id) ON DELETE CASCADE,
    CommentText TEXT NOT NULL,
    CommentType VARCHAR(20) DEFAULT 'Response' CHECK (CommentType IN ('Response', 'Note', 'Internal', 'Customer', 'System')),
    
    -- Author Information
    AuthorId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    AuthorName VARCHAR(200),
    AuthorType VARCHAR(20) DEFAULT 'Staff' CHECK (AuthorType IN ('Staff', 'Customer', 'System')),
    
    -- Attachments
    Attachments TEXT, -- JSON with attachment information
    
    -- Status Changes
    StatusChange VARCHAR(20),
    PreviousStatus VARCHAR(20),
    
    -- Visibility
    IsInternal BOOLEAN DEFAULT false,
    IsVisibleToCustomer BOOLEAN DEFAULT true,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- SupportTicketAttachments Table (files attached to tickets)
CREATE TABLE IF NOT EXISTS SupportTicketAttachments (
    Id SERIAL PRIMARY KEY,
    TicketId INTEGER NOT NULL REFERENCES SupportTickets(Id) ON DELETE CASCADE,
    CommentId INTEGER REFERENCES SupportTicketComments(Id) ON DELETE SET NULL,
    
    -- File Information
    FileName VARCHAR(255) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    FileType VARCHAR(50) NOT NULL,
    MimeType VARCHAR(100),
    
    -- Upload Details
    UploadedBy INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    UploadTime TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Security
    IsPublic BOOLEAN DEFAULT false,
    ExpiresAt TIMESTAMP WITH TIME ZONE
);

-- ========================================
-- FAQ and Knowledge Base Tables
-- ========================================

-- FAQCategories Table (FAQ categorization)
CREATE TABLE IF NOT EXISTS FAQCategories (
    Id SERIAL PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL,
    CategoryCode VARCHAR(20) UNIQUE NOT NULL,
    ParentCategoryId INTEGER REFERENCES FAQCategories(Id) ON DELETE SET NULL,
    Description TEXT,
    DisplayOrder INTEGER DEFAULT 0,
    IsActive BOOLEAN DEFAULT true,
    
    -- System Fields
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- FAQItems Table (frequently asked questions)
CREATE TABLE IF NOT EXISTS FAQItems (
    Id SERIAL PRIMARY KEY,
    Question VARCHAR(500) NOT NULL,
    Answer TEXT NOT NULL,
    
    -- Classification
    CategoryId INTEGER REFERENCES FAQCategories(Id) ON DELETE SET NULL,
    Priority INTEGER DEFAULT 0,
    
    -- Metadata
    Tags TEXT, -- JSON array of tags
    Keywords VARCHAR(500),
    ViewCount INTEGER DEFAULT 0,
    HelpfulCount INTEGER DEFAULT 0,
    
    -- Status
    IsActive BOOLEAN DEFAULT true,
    IsFeatured BOOLEAN DEFAULT false,
    
    -- Access Control
    UserRoleAccess TEXT, -- JSON array of roles
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- User Guides and Documentation Tables
-- ========================================

-- UserGuides Table (step-by-step user guides)
CREATE TABLE IF NOT EXISTS UserGuides (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(200) NOT NULL,
    Slug VARCHAR(200) UNIQUE NOT NULL,
    Description TEXT,
    Content TEXT NOT NULL,
    
    -- Classification
    Category VARCHAR(50),
    Module VARCHAR(50), -- which system module this guide covers
    DifficultyLevel VARCHAR(20) DEFAULT 'Beginner' CHECK (DifficultyLevel IN ('Beginner', 'Intermediate', 'Advanced')),
    GuideType VARCHAR(30) DEFAULT 'Tutorial' CHECK (GuideType IN ('Tutorial', 'Quick Start', 'Reference', 'Troubleshooting', 'Best Practice')),
    
    -- Media and Attachments
    FeaturedImage VARCHAR(500),
    VideoUrl VARCHAR(500),
    Attachments TEXT, -- JSON with attachment information
    
    -- Status and Publishing
    Status VARCHAR(20) DEFAULT 'Draft' CHECK (Status IN ('Draft', 'Review', 'Published', 'Archived')),
    PublishedAt TIMESTAMP WITH TIME ZONE,
    Featured BOOLEAN DEFAULT false,
    
    -- Analytics
    ViewCount INTEGER DEFAULT 0,
    HelpfulCount INTEGER DEFAULT 0,
    LastUpdated TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Access Control
    UserRoleAccess TEXT, -- JSON array of roles
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- System Fields
    AuthorId INTEGER REFERENCES UserAccounts(Id),
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- System Notifications and Alerts Tables
-- ========================================

-- SystemAlerts Table (system-wide notifications and alerts)
CREATE TABLE IF NOT EXISTS SystemAlerts (
    Id SERIAL PRIMARY KEY,
    AlertTitle VARCHAR(200) NOT NULL,
    AlertMessage TEXT NOT NULL,
    
    -- Classification
    AlertType VARCHAR(30) NOT NULL CHECK (AlertType IN ('Maintenance', 'Outage', 'Security', 'Update', 'Reminder', 'Policy')),
    Severity VARCHAR(20) NOT NULL CHECK (Severity IN ('Info', 'Warning', 'Error', 'Critical')),
    Category VARCHAR(50),
    
    -- Targeting
    TargetAudience TEXT, -- JSON array of target roles/users
    TargetTenants TEXT, -- JSON array of target tenant IDs
    TargetBranches TEXT, -- JSON array of target branch IDs
    
    -- Display and Timing
    DisplayType VARCHAR(20) DEFAULT 'Banner' CHECK (DisplayType IN ('Banner', 'Modal', 'Toast', 'Email', 'SMS')),
    IsActive BOOLEAN DEFAULT true,
    StartDateTime TIMESTAMP WITH TIME ZONE,
    EndDateTime TIMESTAMP WITH TIME ZONE,
    
    -- Content
    ActionUrl VARCHAR(500),
    ActionText VARCHAR(100),
    Icon VARCHAR(50),
    Color VARCHAR(20),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- UserAlertAcknowledgments Table (tracking user acknowledgments of alerts)
CREATE TABLE IF NOT EXISTS UserAlertAcknowledgments (
    Id SERIAL PRIMARY KEY,
    AlertId INTEGER NOT NULL REFERENCES SystemAlerts(Id) ON DELETE CASCADE,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    
    -- Acknowledgment Details
    AcknowledgedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    AcknowledgmentType VARCHAR(20) DEFAULT 'Viewed' CHECK (AcknowledgmentType IN ('Viewed', 'Read', 'Dismissed', 'Action Taken')),
    ActionTaken VARCHAR(200),
    
    -- System Fields
    IpAddress VARCHAR(45),
    UserAgent TEXT
);

-- ========================================
-- Indexes for Help and Training Tables
-- ========================================

-- HelpCategories Indexes
CREATE INDEX IF NOT EXISTS idx_helpcategories_code ON HelpCategories(CategoryCode);
CREATE INDEX IF NOT EXISTS idx_helpcategories_parent ON HelpCategories(ParentCategoryId);
CREATE INDEX IF NOT EXISTS idx_helpcategories_tenant ON HelpCategories(TenantId);
CREATE INDEX IF NOT EXISTS idx_helpcategories_active ON HelpCategories(IsActive);

-- HelpArticles Indexes
CREATE INDEX IF NOT EXISTS idx_helparticles_slug ON HelpArticles(Slug);
CREATE INDEX IF NOT EXISTS idx_helparticles_category ON HelpArticles(CategoryId);
CREATE INDEX IF NOT EXISTS idx_helparticles_type ON HelpArticles(ArticleType);
CREATE INDEX IF NOT EXISTS idx_helparticles_status ON HelpArticles(Status);
CREATE INDEX IF NOT EXISTS idx_helparticles_featured ON HelpArticles(Featured);
CREATE INDEX IF NOT EXISTS idx_helparticles_tenant ON HelpArticles(TenantId);
CREATE INDEX IF NOT EXISTS idx_helparticles_author ON HelpArticles(AuthorId);

-- HelpArticleAttachments Indexes
CREATE INDEX IF NOT EXISTS idx_helparticleattachments_article ON HelpArticleAttachments(ArticleId);

-- TrainingCourses Indexes
CREATE INDEX IF NOT EXISTS idx_trainingcourses_code ON TrainingCourses(CourseCode);
CREATE INDEX IF NOT EXISTS idx_trainingcourses_type ON TrainingCourses(CourseType);
CREATE INDEX IF NOT EXISTS idx_trainingcourses_category ON TrainingCourses(Category);
CREATE INDEX IF NOT EXISTS idx_trainingcourses_status ON TrainingCourses(Status);
CREATE INDEX IF NOT EXISTS idx_trainingcourses_tenant ON TrainingCourses(TenantId);
CREATE INDEX IF NOT EXISTS idx_trainingcourses_instructor ON TrainingCourses(InstructorId);

-- TrainingEnrollments Indexes
CREATE INDEX IF NOT EXISTS idx_trainingenrollments_course ON TrainingEnrollments(CourseId);
CREATE INDEX IF NOT EXISTS idx_trainingenrollments_user ON TrainingEnrollments(UserId);
CREATE INDEX IF NOT EXISTS idx_trainingenrollments_tenant ON TrainingEnrollments(TenantId);
CREATE INDEX IF NOT EXISTS idx_trainingenrollments_status ON TrainingEnrollments(EnrollmentStatus);

-- TrainingModules Indexes
CREATE INDEX IF NOT EXISTS idx_trainingmodules_course ON TrainingModules(CourseId);
CREATE INDEX IF NOT EXISTS idx_trainingmodules_parent ON TrainingModules(ParentModuleId);
CREATE INDEX IF NOT EXISTS idx_trainingmodules_type ON TrainingModules(ModuleType);

-- TrainingProgress Indexes
CREATE INDEX IF NOT EXISTS idx_trainingprogress_enrollment ON TrainingProgress(EnrollmentId);
CREATE INDEX IF NOT EXISTS idx_trainingprogress_module ON TrainingProgress(ModuleId);
CREATE INDEX IF NOT EXISTS idx_trainingprogress_user ON TrainingProgress(UserId);
CREATE INDEX IF NOT EXISTS idx_trainingprogress_status ON TrainingProgress(Status);

-- SupportTickets Indexes
CREATE INDEX IF NOT EXISTS idx_supporttickets_number ON SupportTickets(TicketNumber);
CREATE INDEX IF NOT EXISTS idx_supporttickets_user ON SupportTickets(UserId);
CREATE INDEX IF NOT EXISTS idx_supporttickets_tenant ON SupportTickets(TenantId);
CREATE INDEX IF NOT EXISTS idx_supporttickets_branch ON SupportTickets(BranchId);
CREATE INDEX IF NOT EXISTS idx_supporttickets_category ON SupportTickets(Category);
CREATE INDEX IF NOT EXISTS idx_supporttickets_priority ON SupportTickets(Priority);
CREATE INDEX IF NOT EXISTS idx_supporttickets_status ON SupportTickets(Status);
CREATE INDEX IF NOT EXISTS idx_supporttickets_assigned ON SupportTickets(AssignedTo);

-- SupportTicketComments Indexes
CREATE INDEX IF NOT EXISTS idx_supportticketcomments_ticket ON SupportTicketComments(TicketId);
CREATE INDEX IF NOT EXISTS idx_supportticketcomments_author ON SupportTicketComments(AuthorId);
CREATE INDEX IF NOT EXISTS idx_supportticketcomments_type ON SupportTicketComments(CommentType);

-- SupportTicketAttachments Indexes
CREATE INDEX IF NOT EXISTS idx_supportticketattachments_ticket ON SupportTicketAttachments(TicketId);
CREATE INDEX IF NOT EXISTS idx_supportticketattachments_comment ON SupportTicketAttachments(CommentId);

-- FAQCategories Indexes
CREATE INDEX IF NOT EXISTS idx_faqcategories_code ON FAQCategories(CategoryCode);
CREATE INDEX IF NOT EXISTS idx_faqcategories_parent ON FAQCategories(ParentCategoryId);
CREATE INDEX IF NOT EXISTS idx_faqcategories_tenant ON FAQCategories(TenantId);

-- FAQItems Indexes
CREATE INDEX IF NOT EXISTS idx_faqitems_category ON FAQItems(CategoryId);
CREATE INDEX IF NOT EXISTS idx_faqitems_priority ON FAQItems(Priority);
CREATE INDEX IF NOT EXISTS idx_faqitems_featured ON FAQItems(Featured);
CREATE INDEX IF NOT EXISTS idx_faqitems_active ON FAQItems(IsActive);
CREATE INDEX IF NOT EXISTS idx_faqitems_tenant ON FAQItems(TenantId);

-- UserGuides Indexes
CREATE INDEX IF NOT EXISTS idx_userguides_slug ON UserGuides(Slug);
CREATE INDEX IF NOT EXISTS idx_userguides_module ON UserGuides(Module);
CREATE INDEX IF NOT EXISTS idx_userguides_type ON UserGuides(GuideType);
CREATE INDEX IF NOT EXISTS idx_userguides_status ON UserGuides(Status);
CREATE INDEX IF NOT EXISTS idx_userguides_featured ON UserGuides(Featured);
CREATE INDEX IF NOT EXISTS idx_userguides_tenant ON UserGuides(TenantId);
CREATE INDEX IF NOT EXISTS idx_userguides_author ON UserGuides(AuthorId);

-- SystemAlerts Indexes
CREATE INDEX IF NOT EXISTS idx_systemalerts_type ON SystemAlerts(AlertType);
CREATE INDEX IF NOT EXISTS idx_systemalerts_severity ON SystemAlerts(Severity);
CREATE INDEX IF NOT EXISTS idx_systemalerts_active ON SystemAlerts(IsActive);
CREATE INDEX IF NOT EXISTS idx_systemalerts_dates ON SystemAlerts(StartDateTime, EndDateTime);

-- UserAlertAcknowledgments Indexes
CREATE INDEX IF NOT EXISTS idx_useralertacknowledgments_alert ON UserAlertAcknowledgments(AlertId);
CREATE INDEX IF NOT EXISTS idx_useralertacknowledgments_user ON UserAlertAcknowledgments(UserId);

-- ========================================
-- Triggers for Help and Training Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to help and training tables
CREATE TRIGGER update_helpcategories_updated_at BEFORE UPDATE ON HelpCategories 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_helparticles_updated_at BEFORE UPDATE ON HelpArticles 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_trainingcourses_updated_at BEFORE UPDATE ON TrainingCourses 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_trainingenrollments_updated_at BEFORE UPDATE ON TrainingEnrollments 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_trainingmodules_updated_at BEFORE UPDATE ON TrainingModules 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_trainingprogress_updated_at BEFORE UPDATE ON TrainingProgress 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_supporttickets_updated_at BEFORE UPDATE ON SupportTickets 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_faqcategories_updated_at BEFORE UPDATE ON FAQCategories 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_faqitems_updated_at BEFORE UPDATE ON FAQItems 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_userguides_updated_at BEFORE UPDATE ON UserGuides 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_systemalerts_updated_at BEFORE UPDATE ON SystemAlerts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to generate ticket numbers
CREATE OR REPLACE FUNCTION generate_ticket_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.TicketNumber := 'TCK-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP)::text, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_ticket_number_trigger
    BEFORE INSERT ON SupportTickets
    FOR EACH ROW EXECUTE FUNCTION generate_ticket_number();

-- ========================================
-- Views for Help and Training Management
-- ========================================

-- View for Help Article Summary
CREATE OR REPLACE VIEW HelpArticleSummary AS
SELECT 
    ha.Id,
    ha.Title,
    ha.Slug,
    ha.Summary,
    hc.CategoryName,
    ha.ArticleType,
    ha.DifficultyLevel,
    ha.Status,
    ha.ViewCount,
    ha.HelpfulCount,
    ha.PublishedAt,
    u.FirstName || ' ' || u.LastName as AuthorName,
    t.BusinessName as TenantName
FROM HelpArticles ha
LEFT JOIN HelpCategories hc ON ha.CategoryId = hc.Id
LEFT JOIN UserAccounts u ON ha.AuthorId = u.Id
LEFT JOIN Tenants t ON ha.TenantId = t.Id
WHERE ha.Status = 'Published'
ORDER BY ha.PublishedAt DESC;

-- View for Training Course Summary
CREATE OR REPLACE VIEW TrainingCourseSummary AS
SELECT 
    tc.Id,
    tc.CourseName,
    tc.CourseCode,
    tc.Description,
    tc.Category,
    tc.DifficultyLevel,
    tc.DurationMinutes,
    tc.MaxParticipants,
    tc.CurrentEnrollment,
    tc.Status,
    tc.StartDate,
    tc.EndDate,
    u.FirstName || ' ' || u.LastName as InstructorName,
    t.BusinessName as TenantName,
    CASE 
        WHEN tc.StartDate > CURRENT_DATE THEN 'Upcoming'
        WHEN tc.EndDate >= CURRENT_DATE THEN 'In Progress'
        WHEN tc.EndDate < CURRENT_DATE THEN 'Completed'
        ELSE 'Not Scheduled'
    END as CourseStatus
FROM TrainingCourses tc
LEFT JOIN UserAccounts u ON tc.InstructorId = u.Id
LEFT JOIN Tenants t ON tc.TenantId = t.Id
WHERE tc.IsActive = true
ORDER BY tc.StartDate ASC;

-- View for Support Ticket Dashboard
CREATE OR REPLACE VIEW SupportTicketDashboard AS
SELECT 
    st.Id,
    st.TicketNumber,
    st.Subject,
    st.Category,
    st.Priority,
    st.Severity,
    st.Status,
    st.CreatedAt,
    u.FirstName || ' ' || u.LastName as AssignedToName,
    customer.FirstName || ' ' || customer.LastName as CustomerName,
    t.BusinessName as TenantName,
    b.BranchName as BranchName,
    CASE 
        WHEN st.Status = 'Open' THEN 'Open'
        WHEN st.Status = 'In Progress' THEN 'In Progress'
        WHEN st.Status = 'Resolved' THEN 'Resolved'
        ELSE st.Status
    END as StatusIndicator,
    EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - st.CreatedAt)) / 3600 as HoursOpen
FROM SupportTickets st
LEFT JOIN UserAccounts u ON st.AssignedTo = u.Id
LEFT JOIN UserAccounts customer ON st.UserId = customer.Id
LEFT JOIN Tenants t ON st.TenantId = t.Id
LEFT JOIN Branches b ON st.BranchId = b.Id
ORDER BY 
    CASE 
        WHEN st.Priority = 'Critical' THEN 1
        WHEN st.Priority = 'Urgent' THEN 2
        WHEN st.Priority = 'High' THEN 3
        WHEN st.Priority = 'Normal' THEN 4
        ELSE 5
    END,
    st.CreatedAt DESC;

-- View for User Training Progress
CREATE OR REPLACE VIEW UserTrainingProgress AS
SELECT 
    u.Id as UserId,
    u.FirstName || ' ' || u.LastName as UserName,
    tc.CourseName,
    tc.CourseCode,
    te.EnrollmentDate,
    te.EnrollmentStatus,
    te.ProgressPercentage,
    te.CompletionDate,
    te.Score,
    te.Passed,
    te.CertificateIssued,
    COUNT(DISTINCT tm.Id) as TotalModules,
    COUNT(DISTINCT CASE WHEN tp.Status = 'Completed' THEN tp.ModuleId END) as CompletedModules,
    t.BusinessName as TenantName
FROM UserAccounts u
JOIN TrainingEnrollments te ON u.Id = te.UserId
JOIN TrainingCourses tc ON te.CourseId = tc.Id
LEFT JOIN TrainingProgress tp ON te.Id = tp.EnrollmentId
LEFT JOIN TrainingModules tm ON tc.Id = tm.CourseId
LEFT JOIN Tenants t ON te.TenantId = t.Id
GROUP BY u.Id, u.FirstName, u.LastName, tc.CourseName, tc.CourseCode, 
         te.EnrollmentDate, te.EnrollmentStatus, te.ProgressPercentage, 
         te.CompletionDate, te.Score, te.Passed, te.CertificateIssued, t.BusinessName
ORDER BY te.EnrollmentDate DESC;
