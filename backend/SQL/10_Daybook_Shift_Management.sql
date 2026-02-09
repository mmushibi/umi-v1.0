-- Daybook and Shift Management Tables
-- This file contains SQL for daybook operations, shift management, and daily business tracking

-- ========================================
-- Shift Management Tables
-- ========================================

-- Shifts Table (comprehensive shift management)
CREATE TABLE IF NOT EXISTS Shifts (
    Id SERIAL PRIMARY KEY,
    ShiftName VARCHAR(100) NOT NULL,
    ShiftType VARCHAR(20) NOT NULL CHECK (ShiftType IN ('Regular', 'Weekend', 'Holiday', 'Night', 'Special')),
    ShiftCode VARCHAR(20) UNIQUE NOT NULL,
    
    -- Schedule Information
    ScheduledStart TIME NOT NULL,
    ScheduledEnd TIME NOT NULL,
    ScheduledDuration INTEGER GENERATED ALWAYS AS (EXTRACT(EPOCH FROM (ScheduledEnd - ScheduledStart)) / 60) STORED, -- in minutes
    BreakDuration INTEGER DEFAULT 60, -- in minutes
    EffectiveDuration INTEGER GENERATED ALWAYS AS (ScheduledDuration - BreakDuration) STORED,
    
    -- Days of Week
    Monday BOOLEAN DEFAULT false,
    Tuesday BOOLEAN DEFAULT false,
    Wednesday BOOLEAN DEFAULT false,
    Thursday BOOLEAN DEFAULT false,
    Friday BOOLEAN DEFAULT false,
    Saturday BOOLEAN DEFAULT false,
    Sunday BOOLEAN DEFAULT false,
    
    -- Staffing Requirements
    MinimumStaff INTEGER DEFAULT 1,
    MaximumStaff INTEGER DEFAULT 5,
    RequiredRoles TEXT, -- JSON array of required roles
    
    -- Business Rules
    IsOvernight BOOLEAN DEFAULT false,
    IsPeakShift BOOLEAN DEFAULT false,
    PremiumRate DECIMAL(5,2) DEFAULT 0.00, -- additional percentage for premium shifts
    
    -- System Fields
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    IsActive BOOLEAN DEFAULT true,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ShiftAssignments Table (assigning users to shifts)
CREATE TABLE IF NOT EXISTS ShiftAssignments (
    Id SERIAL PRIMARY KEY,
    ShiftId INTEGER NOT NULL REFERENCES Shifts(Id) ON DELETE CASCADE,
    UserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Assignment Details
    AssignmentDate DATE NOT NULL,
    Role VARCHAR(50), -- specific role for this shift
    Position VARCHAR(100), -- specific position (e.g., 'Head Cashier', 'Senior Pharmacist')
    
    -- Status
    AssignmentStatus VARCHAR(20) DEFAULT 'Scheduled' CHECK (AssignmentStatus IN ('Scheduled', 'Confirmed', 'InProgress', 'Completed', 'Absent', 'Late', 'LeftEarly', 'Replaced')),
    
    -- Time Tracking
    ScheduledStart TIMESTAMP WITH TIME ZONE,
    ScheduledEnd TIMESTAMP WITH TIME ZONE,
    ActualStart TIMESTAMP WITH TIME ZONE,
    ActualEnd TIMESTAMP WITH TIME ZONE,
    BreakStart TIMESTAMP WITH TIME ZONE,
    BreakEnd TIMESTAMP WITH TIME ZONE,
    
    -- Performance Metrics
    PunctualityStatus VARCHAR(20) CHECK (PunctualityStatus IN ('On Time', 'Late', 'Very Late', 'Early', 'Absent')),
    LateMinutes INTEGER DEFAULT 0,
    EarlyDepartureMinutes INTEGER DEFAULT 0,
    TotalWorkedMinutes INTEGER GENERATED ALWAYS AS (
        CASE 
            WHEN ActualStart IS NOT NULL AND ActualEnd IS NOT NULL 
            THEN EXTRACT(EPOCH FROM (ActualEnd - ActualStart)) / 60
            ELSE 0 
        END
    ) STORED,
    
    -- Approval and Notes
    ApprovedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    RejectionReason VARCHAR(500),
    Notes VARCHAR(1000),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ShiftReplacements Table (shift replacement requests and approvals)
CREATE TABLE IF NOT EXISTS ShiftReplacements (
    Id SERIAL PRIMARY KEY,
    OriginalAssignmentId INTEGER NOT NULL REFERENCES ShiftAssignments(Id) ON DELETE CASCADE,
    OriginalUserId INTEGER NOT NULL REFERENCES UserAccounts(Id) ON DELETE CASCADE,
    ReplacementUserId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    
    -- Request Details
    RequestType VARCHAR(20) NOT NULL CHECK (RequestType IN ('Swap', 'Cover', 'Emergency', 'Planned')),
    RequestReason VARCHAR(200) NOT NULL,
    RequestDate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Coverage Details
    CoverageDate DATE NOT NULL,
    CoverageStart TIMESTAMP WITH TIME ZONE,
    CoverageEnd TIMESTAMP WITH TIME ZONE,
    
    -- Status
    Status VARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Completed', 'Cancelled')),
    ApprovedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    RejectionReason VARCHAR(500),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Daybook and Daily Operations Tables
-- ========================================

-- Daybook Table (daily business summary and tracking)
CREATE TABLE IF NOT EXISTS Daybook (
    Id SERIAL PRIMARY KEY,
    DaybookDate DATE NOT NULL UNIQUE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Business Hours
    OpeningTime TIME,
    ClosingTime TIME,
    BusinessHours INTEGER, -- in minutes
    IsBusinessDay BOOLEAN DEFAULT true,
    IsHoliday BOOLEAN DEFAULT false,
    HolidayName VARCHAR(100),
    
    -- Staffing Summary
    ScheduledStaff INTEGER DEFAULT 0,
    ActualStaff INTEGER DEFAULT 0,
    AbsentStaff INTEGER DEFAULT 0,
    OvertimeHours DECIMAL(5,2) DEFAULT 0.00,
    
    -- Financial Summary
    OpeningCashBalance DECIMAL(15,2) DEFAULT 0.00,
    ClosingCashBalance DECIMAL(15,2) DEFAULT 0.00,
    CashSales DECIMAL(15,2) DEFAULT 0.00,
    CardSales DECIMAL(15,2) DEFAULT 0.00,
    MobileMoneySales DECIMAL(15,2) DEFAULT 0.00,
    CreditSales DECIMAL(15,2) DEFAULT 0.00,
    TotalSales DECIMAL(15,2) DEFAULT 0.00,
    
    -- Transaction Summary
    TotalTransactions INTEGER DEFAULT 0,
    CashTransactions INTEGER DEFAULT 0,
    CardTransactions INTEGER DEFAULT 0,
    MobileMoneyTransactions INTEGER DEFAULT 0,
    CreditTransactions INTEGER DEFAULT 0,
    RefundTransactions INTEGER DEFAULT 0,
    
    -- Payment Method Breakdown
    CashReceived DECIMAL(15,2) DEFAULT 0.00,
    CashPaidOut DECIMAL(15,2) DEFAULT 0.00,
    CardReceived DECIMAL(15,2) DEFAULT 0.00,
    MobileMoneyReceived DECIMAL(15,2) DEFAULT 0.00,
    
    -- Tax Summary
    TotalTax DECIMAL(15,2) DEFAULT 0.00,
    VatAmount DECIMAL(15,2) DEFAULT 0.00,
    OtherTaxes DECIMAL(15,2) DEFAULT 0.00,
    
    -- Discount Summary
    TotalDiscount DECIMAL(15,2) DEFAULT 0.00,
    StaffDiscount DECIMAL(15,2) DEFAULT 0.00,
    PromotionDiscount DECIMAL(15,2) DEFAULT 0.00,
    
    -- Reconciliation
    ExpectedCash DECIMAL(15,2) DEFAULT 0.00,
    ActualCash DECIMAL(15,2) DEFAULT 0.00,
    CashVariance DECIMAL(15,2) DEFAULT 0.00,
    VarianceReason VARCHAR(500),
    
    -- Inventory Summary
    OpeningInventoryValue DECIMAL(15,2) DEFAULT 0.00,
    ClosingInventoryValue DECIMAL(15,2) DEFAULT 0.00,
    CostOfGoodsSold DECIMAL(15,2) DEFAULT 0.00,
    GrossProfit DECIMAL(15,2) DEFAULT 0.00,
    
    -- Status
    DaybookStatus VARCHAR(20) DEFAULT 'Open' CHECK (DaybookStatus IN ('Open', 'Closed', 'Reconciled', 'Adjusted')),
    ClosedBy INTEGER REFERENCES UserAccounts(Id),
    ClosedAt TIMESTAMP WITH TIME ZONE,
    ReconciledBy INTEGER REFERENCES UserAccounts(Id),
    ReconciledAt TIMESTAMP WITH TIME ZONE,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- DaybookTransactions Table (detailed transaction logging for daybook)
CREATE TABLE IF NOT EXISTS DaybookTransactions (
    Id SERIAL PRIMARY KEY,
    DaybookId INTEGER NOT NULL REFERENCES Daybook(Id) ON DELETE CASCADE,
    TransactionTime TIMESTAMP WITH TIME ZONE NOT NULL,
    
    -- Transaction Details
    TransactionType VARCHAR(30) NOT NULL CHECK (TransactionType IN ('Sale', 'Refund', 'Cash In', 'Cash Out', 'Adjustment', 'Opening', 'Closing')),
    TransactionCategory VARCHAR(50), -- 'Sales', 'Banking', 'Expenses', 'Adjustments'
    ReferenceType VARCHAR(50), -- 'Sale', 'Purchase', 'Expense', etc.
    ReferenceId INTEGER,
    ReferenceNumber VARCHAR(100),
    
    -- Financial Details
    Amount DECIMAL(15,2) NOT NULL,
    PaymentMethod VARCHAR(20), -- 'Cash', 'Card', 'Mobile Money', etc.
    Currency VARCHAR(10) DEFAULT 'ZMW',
    
    -- Description and Context
    Description VARCHAR(500) NOT NULL,
    Category VARCHAR(50),
    Subcategory VARCHAR(50),
    
    -- User Information
    UserId INTEGER REFERENCES UserAccounts(Id),
    UserName VARCHAR(200),
    ShiftId INTEGER REFERENCES ShiftAssignments(Id),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Cash Management Tables
-- ========================================

-- CashDrawers Table (cash drawer management for shifts)
CREATE TABLE IF NOT EXISTS CashDrawers (
    Id SERIAL PRIMARY KEY,
    DrawerNumber VARCHAR(20) UNIQUE NOT NULL,
    DrawerName VARCHAR(100),
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Assignment
    AssignedUserId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    AssignedShiftId INTEGER REFERENCES ShiftAssignments(Id) ON DELETE SET NULL,
    
    -- Cash Management
    OpeningBalance DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    ClosingBalance DECIMAL(15,2) DEFAULT 0.00,
    ExpectedClosingBalance DECIMAL(15,2) DEFAULT 0.00,
    CashVariance DECIMAL(15,2) DEFAULT 0.00,
    
    -- Transaction Totals
    CashSales DECIMAL(15,2) DEFAULT 0.00,
    CashRefunds DECIMAL(15,2) DEFAULT 0.00,
    CashPaidIn DECIMAL(15,2) DEFAULT 0.00,
    CashPaidOut DECIMAL(15,2) DEFAULT 0.00,
    
    -- Non-Cash Transactions
    CardSales DECIMAL(15,2) DEFAULT 0.00,
    MobileMoneySales DECIMAL(15,2) DEFAULT 0.00,
    CreditSales DECIMAL(15,2) DEFAULT 0.00,
    
    -- Status and Timing
    Status VARCHAR(20) DEFAULT 'Closed' CHECK (Status IN ('Open', 'Closed', 'Reconciled', 'Adjusted')),
    OpenedAt TIMESTAMP WITH TIME ZONE,
    ClosedAt TIMESTAMP WITH TIME ZONE,
    ReconciledAt TIMESTAMP WITH TIME ZONE,
    
    -- Approval and Notes
    OpenedBy INTEGER REFERENCES UserAccounts(Id),
    ClosedBy INTEGER REFERENCES UserAccounts(Id),
    ReconciledBy INTEGER REFERENCES UserAccounts(Id),
    OpeningNotes VARCHAR(500),
    ClosingNotes VARCHAR(500),
    ReconciliationNotes VARCHAR(500),
    VarianceExplanation VARCHAR(500),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- CashMovements Table (individual cash movements)
CREATE TABLE IF NOT EXISTS CashMovements (
    Id SERIAL PRIMARY KEY,
    CashDrawerId INTEGER NOT NULL REFERENCES CashDrawers(Id) ON DELETE CASCADE,
    MovementTime TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Movement Details
    MovementType VARCHAR(20) NOT NULL CHECK (MovementType IN ('Sale', 'Refund', 'Cash In', 'Cash Out', 'Opening', 'Closing', 'Adjustment')),
    MovementCategory VARCHAR(50), -- 'Sales', 'Banking', 'Expenses', 'Safe Transfer', etc.
    Amount DECIMAL(15,2) NOT NULL,
    
    -- Reference Information
    ReferenceType VARCHAR(50), -- 'Sale', 'Purchase', 'Expense', etc.
    ReferenceId INTEGER,
    ReferenceNumber VARCHAR(100),
    
    -- Description and Approval
    Description VARCHAR(500) NOT NULL,
    Reason VARCHAR(200),
    ApprovedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovalRequired BOOLEAN DEFAULT false,
    
    -- User Information
    UserId INTEGER REFERENCES UserAccounts(Id),
    UserName VARCHAR(200),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Daily Reports and Summaries Tables
-- ========================================

-- DailySalesSummary Table (aggregated daily sales data)
CREATE TABLE IF NOT EXISTS DailySalesSummary (
    Id SERIAL PRIMARY KEY,
    SummaryDate DATE NOT NULL UNIQUE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Sales Metrics
    TotalSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalTransactions INTEGER NOT NULL DEFAULT 0,
    AverageTransactionValue DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    
    -- Payment Method Breakdown
    CashSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CardSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    MobileMoneySales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CreditSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    OtherSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    
    -- Transaction Counts
    CashTransactions INTEGER NOT NULL DEFAULT 0,
    CardTransactions INTEGER NOT NULL DEFAULT 0,
    MobileMoneyTransactions INTEGER NOT NULL DEFAULT 0,
    CreditTransactions INTEGER NOT NULL DEFAULT 0,
    OtherTransactions INTEGER NOT NULL DEFAULT 0,
    
    -- Financial Metrics
    TotalTax DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalDiscount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    NetSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    
    -- Customer Metrics
    UniqueCustomers INTEGER NOT NULL DEFAULT 0,
    NewCustomers INTEGER NOT NULL DEFAULT 0,
    ReturningCustomers INTEGER NOT NULL DEFAULT 0,
    
    -- Product Metrics
    UniqueProductsSold INTEGER NOT NULL DEFAULT 0,
    TopSellingCategory VARCHAR(100),
    TopSellingProduct VARCHAR(200),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- DailyInventorySummary Table (daily inventory movements)
CREATE TABLE IF NOT EXISTS DailyInventorySummary (
    Id SERIAL PRIMARY KEY,
    SummaryDate DATE NOT NULL UNIQUE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Inventory Values
    OpeningInventoryValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    ClosingInventoryValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CostOfGoodsSold DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    
    -- Inventory Movements
    PurchasesValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    SalesValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    ReturnsValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    AdjustmentsValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TransfersValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    
    -- Stock Metrics
    TotalProducts INTEGER NOT NULL DEFAULT 0,
    LowStockProducts INTEGER NOT NULL DEFAULT 0,
    OutOfStockProducts INTEGER NOT NULL DEFAULT 0,
    ExpiredProducts INTEGER NOT NULL DEFAULT 0,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Shift Performance and Analytics Tables
-- ========================================

-- ShiftPerformanceMetrics Table (shift performance tracking)
CREATE TABLE IF NOT EXISTS ShiftPerformanceMetrics (
    Id SERIAL PRIMARY KEY,
    ShiftAssignmentId INTEGER NOT NULL REFERENCES ShiftAssignments(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Performance Metrics
    SalesAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TransactionCount INTEGER NOT NULL DEFAULT 0,
    AverageTransactionValue DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CustomerCount INTEGER NOT NULL DEFAULT 0,
    NewCustomerCount INTEGER NOT NULL DEFAULT 0,
    
    -- Efficiency Metrics
    TransactionsPerHour DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    SalesPerHour DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CustomerServiceRating DECIMAL(3,2), -- 1-5 scale
    
    -- Cash Handling
    CashHandled DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CashVariance DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    VariancePercentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    
    -- Compliance Metrics
    ComplianceScore DECIMAL(5,2) NOT NULL DEFAULT 100.00,
    ProcedureAdherence DECIMAL(5,2) NOT NULL DEFAULT 100.00,
    ErrorCount INTEGER NOT NULL DEFAULT 0,
    
    -- System Fields
    RecordedBy INTEGER REFERENCES UserAccounts(Id),
    RecordedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ShiftIncidents Table (shift-related incidents and issues)
CREATE TABLE IF NOT EXISTS ShiftIncidents (
    Id SERIAL PRIMARY KEY,
    ShiftAssignmentId INTEGER REFERENCES ShiftAssignments(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE CASCADE,
    
    -- Incident Details
    IncidentTime TIMESTAMP WITH TIME ZONE NOT NULL,
    IncidentType VARCHAR(50) NOT NULL CHECK (IncidentType IN ('Cash Shortage', 'Cash Overage', 'System Error', 'Customer Complaint', 'Staff Issue', 'Security', 'Equipment Failure', 'Other')),
    Severity VARCHAR(20) NOT NULL CHECK (Severity IN ('Low', 'Medium', 'High', 'Critical')),
    Description TEXT NOT NULL,
    
    -- Impact Assessment
    FinancialImpact DECIMAL(15,2) DEFAULT 0.00,
    OperationalImpact VARCHAR(200),
    CustomerImpact VARCHAR(200),
    
    -- Resolution Information
    ResolutionStatus VARCHAR(20) DEFAULT 'Open' CHECK (ResolutionStatus IN ('Open', 'In Progress', 'Resolved', 'Escalated')),
    ResolutionTime TIMESTAMP WITH TIME ZONE,
    ResolutionDetails TEXT,
    PreventiveActions TEXT,
    
    -- Reporting and Follow-up
    ReportedBy INTEGER REFERENCES UserAccounts(Id),
    AssignedTo INTEGER REFERENCES UserAccounts(Id),
    FollowUpRequired BOOLEAN DEFAULT false,
    FollowUpDate DATE,
    FollowUpCompleted BOOLEAN DEFAULT false,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Daybook and Shift Management Tables
-- ========================================

-- Shifts Indexes
CREATE INDEX IF NOT EXISTS idx_shifts_code ON Shifts(ShiftCode);
CREATE INDEX IF NOT EXISTS idx_shifts_type ON Shifts(ShiftType);
CREATE INDEX IF NOT EXISTS idx_shifts_tenant ON Shifts(TenantId);
CREATE INDEX IF NOT EXISTS idx_shifts_branch ON Shifts(BranchId);
CREATE INDEX IF NOT EXISTS idx_shifts_active ON Shifts(IsActive);

-- ShiftAssignments Indexes
CREATE INDEX IF NOT EXISTS idx_shiftassignments_shift ON ShiftAssignments(ShiftId);
CREATE INDEX IF NOT EXISTS idx_shiftassignments_user ON ShiftAssignments(UserId);
CREATE INDEX IF NOT EXISTS idx_shiftassignments_tenant ON ShiftAssignments(TenantId);
CREATE INDEX IF NOT EXISTS idx_shiftassignments_branch ON ShiftAssignments(BranchId);
CREATE INDEX IF NOT EXISTS idx_shiftassignments_date ON ShiftAssignments(AssignmentDate);
CREATE INDEX IF NOT EXISTS idx_shiftassignments_status ON ShiftAssignments(AssignmentStatus);

-- ShiftReplacements Indexes
CREATE INDEX IF NOT EXISTS idx_shiftreplacements_original ON ShiftReplacements(OriginalAssignmentId);
CREATE INDEX IF NOT EXISTS idx_shiftreplacements_originaluser ON ShiftReplacements(OriginalUserId);
CREATE INDEX IF NOT EXISTS idx_shiftreplacements_replacement ON ShiftReplacements(ReplacementUserId);
CREATE INDEX IF NOT EXISTS idx_shiftreplacements_status ON ShiftReplacements(Status);

-- Daybook Indexes
CREATE INDEX IF NOT EXISTS idx_daybook_date ON Daybook(DaybookDate);
CREATE INDEX IF NOT EXISTS idx_daybook_tenant ON Daybook(TenantId);
CREATE INDEX IF NOT EXISTS idx_daybook_branch ON Daybook(BranchId);
CREATE INDEX IF NOT EXISTS idx_daybook_status ON Daybook(DaybookStatus);

-- DaybookTransactions Indexes
CREATE INDEX IF NOT EXISTS idx_daybooktransactions_daybook ON DaybookTransactions(DaybookId);
CREATE INDEX IF NOT EXISTS idx_daybooktransactions_time ON DaybookTransactions(TransactionTime);
CREATE INDEX IF NOT EXISTS idx_daybooktransactions_type ON DaybookTransactions(TransactionType);
CREATE INDEX IF NOT EXISTS idx_daybooktransactions_user ON DaybookTransactions(UserId);

-- CashDrawers Indexes
CREATE INDEX IF NOT EXISTS idx_cashdrawers_number ON CashDrawers(DrawerNumber);
CREATE INDEX IF NOT EXISTS idx_cashdrawers_tenant ON CashDrawers(TenantId);
CREATE INDEX IF NOT EXISTS idx_cashdrawers_branch ON CashDrawers(BranchId);
CREATE INDEX IF NOT EXISTS idx_cashdrawers_assigneduser ON CashDrawers(AssignedUserId);
CREATE INDEX IF NOT EXISTS idx_cashdrawers_status ON CashDrawers(Status);

-- CashMovements Indexes
CREATE INDEX IF NOT EXISTS idx_cashmovements_drawer ON CashMovements(CashDrawerId);
CREATE INDEX IF NOT EXISTS idx_cashmovements_time ON CashMovements(MovementTime);
CREATE INDEX IF NOT EXISTS idx_cashmovements_type ON CashMovements(MovementType);
CREATE INDEX IF NOT EXISTS idx_cashmovements_user ON CashMovements(UserId);

-- DailySalesSummary Indexes
CREATE INDEX IF NOT EXISTS idx_dailysalessummary_date ON DailySalesSummary(SummaryDate);
CREATE INDEX IF NOT EXISTS idx_dailysalessummary_tenant ON DailySalesSummary(TenantId);
CREATE INDEX IF NOT EXISTS idx_dailysalessummary_branch ON DailySalesSummary(BranchId);

-- DailyInventorySummary Indexes
CREATE INDEX IF NOT EXISTS idx_dailyinventorysummary_date ON DailyInventorySummary(SummaryDate);
CREATE INDEX IF NOT EXISTS idx_dailyinventorysummary_tenant ON DailyInventorySummary(TenantId);
CREATE INDEX IF NOT EXISTS idx_dailyinventorysummary_branch ON DailyInventorySummary(BranchId);

-- ShiftPerformanceMetrics Indexes
CREATE INDEX IF NOT EXISTS idx_shiftperformancemetrics_assignment ON ShiftPerformanceMetrics(ShiftAssignmentId);
CREATE INDEX IF NOT EXISTS idx_shiftperformancemetrics_tenant ON ShiftPerformanceMetrics(TenantId);
CREATE INDEX IF NOT EXISTS idx_shiftperformancemetrics_branch ON ShiftPerformanceMetrics(BranchId);

-- ShiftIncidents Indexes
CREATE INDEX IF NOT EXISTS idx_shiftincidents_assignment ON ShiftIncidents(ShiftAssignmentId);
CREATE INDEX IF NOT EXISTS idx_shiftincidents_tenant ON ShiftIncidents(TenantId);
CREATE INDEX IF NOT EXISTS idx_shiftincidents_branch ON ShiftIncidents(BranchId);
CREATE INDEX IF NOT EXISTS idx_shiftincidents_type ON ShiftIncidents(IncidentType);
CREATE INDEX IF NOT EXISTS idx_shiftincidents_severity ON ShiftIncidents(Severity);
CREATE INDEX IF NOT EXISTS idx_shiftincidents_status ON ShiftIncidents(ResolutionStatus);

-- ========================================
-- Triggers for Daybook and Shift Management Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to daybook and shift tables
CREATE TRIGGER update_shifts_updated_at BEFORE UPDATE ON Shifts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_shiftassignments_updated_at BEFORE UPDATE ON ShiftAssignments 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_shiftreplacements_updated_at BEFORE UPDATE ON ShiftReplacements 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_daybook_updated_at BEFORE UPDATE ON Daybook 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cashdrawers_updated_at BEFORE UPDATE ON CashDrawers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_dailysalessummary_updated_at BEFORE UPDATE ON DailySalesSummary 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_dailyinventorysummary_updated_at BEFORE UPDATE ON DailyInventorySummary 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_shiftincidents_updated_at BEFORE UPDATE ON ShiftIncidents 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to calculate cash variance
CREATE OR REPLACE FUNCTION calculate_cash_variance()
RETURNS TRIGGER AS $$
BEGIN
    NEW.ExpectedClosingBalance := NEW.OpeningBalance + NEW.CashSales - NEW.CashRefunds + NEW.CashPaidIn - NEW.CashPaidOut;
    NEW.CashVariance := NEW.ClosingBalance - NEW.ExpectedClosingBalance;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER calculate_cash_variance_trigger
    BEFORE INSERT OR UPDATE ON CashDrawers
    FOR EACH ROW EXECUTE FUNCTION calculate_cash_variance();

-- ========================================
-- Views for Daybook and Shift Management
-- ========================================

-- View for Shift Schedule Overview
CREATE OR REPLACE VIEW ShiftScheduleOverview AS
SELECT 
    s.Id as ShiftId,
    s.ShiftName,
    s.ShiftCode,
    s.ScheduledStart,
    s.ScheduledEnd,
    s.ScheduledDuration,
    s.MinimumStaff,
    s.MaximumStaff,
    s.TenantId,
    t.BusinessName as TenantName,
    s.BranchId,
    b.Name as BranchName,
    s.IsActive,
    CASE 
        WHEN s.Monday THEN 'Mon'
        WHEN s.Tuesday THEN 'Tue'
        WHEN s.Wednesday THEN 'Wed'
        WHEN s.Thursday THEN 'Thu'
        WHEN s.Friday THEN 'Fri'
        WHEN s.Saturday THEN 'Sat'
        WHEN s.Sunday THEN 'Sun'
    END as ScheduledDays
FROM Shifts s
LEFT JOIN Tenants t ON s.TenantId = t.Id
LEFT JOIN Branches b ON s.BranchId = b.Id
WHERE s.IsActive = true;

-- View for Today's Shift Assignments
CREATE OR REPLACE VIEW TodaysShiftAssignments AS
SELECT 
    sa.Id,
    sa.AssignmentDate,
    s.ShiftName,
    u.FirstName || ' ' || u.LastName as EmployeeName,
    u.Username,
    sa.Role,
    sa.AssignmentStatus,
    sa.ScheduledStart,
    sa.ScheduledEnd,
    sa.ActualStart,
    sa.ActualEnd,
    sa.PunctualityStatus,
    sa.TotalWorkedMinutes,
    sa.Notes,
    t.BusinessName as TenantName,
    b.Name as BranchName
FROM ShiftAssignments sa
JOIN Shifts s ON sa.ShiftId = s.Id
JOIN UserAccounts u ON sa.UserId = u.Id
LEFT JOIN Tenants t ON sa.TenantId = t.Id
LEFT JOIN Branches b ON sa.BranchId = b.Id
WHERE sa.AssignmentDate = CURRENT_DATE
ORDER BY sa.ScheduledStart;

-- View for Daybook Summary
CREATE OR REPLACE VIEW DaybookSummary AS
SELECT 
    d.Id,
    d.DaybookDate,
    d.BusinessHours,
    d.TotalSales,
    d.TotalTransactions,
    d.AverageTransactionValue,
    d.CashSales,
    d.CardSales,
    d.MobileMoneySales,
    d.CashVariance,
    d.DaybookStatus,
    d.ClosingNotes,
    t.BusinessName as TenantName,
    b.Name as BranchName,
    CASE 
        WHEN d.CashVariance > 0 THEN 'Over'
        WHEN d.CashVariance < 0 THEN 'Short'
        ELSE 'Balanced'
    END as CashStatus
FROM Daybook d
LEFT JOIN Tenants t ON d.TenantId = t.Id
LEFT JOIN Branches b ON d.BranchId = b.Id
ORDER BY d.DaybookDate DESC;

-- View for Shift Performance Dashboard
CREATE OR REPLACE VIEW ShiftPerformanceDashboard AS
SELECT 
    sa.Id as AssignmentId,
    sa.AssignmentDate,
    u.FirstName || ' ' || u.LastName as EmployeeName,
    s.ShiftName,
    spm.SalesAmount,
    spm.TransactionCount,
    spm.AverageTransactionValue,
    spm.CustomerCount,
    spm.TransactionsPerHour,
    spm.SalesPerHour,
    spm.CashVariance,
    spm.ComplianceScore,
    COUNT(DISTINCT si.Id) as IncidentCount,
    t.BusinessName as TenantName,
    b.Name as BranchName
FROM ShiftAssignments sa
JOIN UserAccounts u ON sa.UserId = u.Id
JOIN Shifts s ON sa.ShiftId = s.Id
LEFT JOIN ShiftPerformanceMetrics spm ON sa.Id = spm.ShiftAssignmentId
LEFT JOIN ShiftIncidents si ON sa.Id = si.ShiftAssignmentId
LEFT JOIN Tenants t ON sa.TenantId = t.Id
LEFT JOIN Branches b ON sa.BranchId = b.Id
WHERE sa.AssignmentDate >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY sa.Id, sa.AssignmentDate, u.FirstName, u.LastName, s.ShiftName, 
         spm.SalesAmount, spm.TransactionCount, spm.AverageTransactionValue, 
         spm.CustomerCount, spm.TransactionsPerHour, spm.SalesPerHour, 
         spm.CashVariance, spm.ComplianceScore, t.BusinessName, b.Name
ORDER BY sa.AssignmentDate DESC, s.ScheduledStart;
