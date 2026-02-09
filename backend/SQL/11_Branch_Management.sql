-- Branch Management Tables
-- This file contains SQL for multi-branch management, branch operations, and inter-branch transactions

-- ========================================
-- Branch Master Tables
-- ========================================

-- Branches Table (comprehensive branch information)
CREATE TABLE IF NOT EXISTS Branches (
    Id SERIAL PRIMARY KEY,
    BranchCode VARCHAR(20) UNIQUE NOT NULL,
    BranchName VARCHAR(200) NOT NULL,
    TradeName VARCHAR(200),
    
    -- Contact Information
    PhoneNumber VARCHAR(20),
    AlternativePhoneNumber VARCHAR(20),
    Email VARCHAR(100),
    Website VARCHAR(200),
    
    -- Address Information
    PhysicalAddress TEXT NOT NULL,
    PostalAddress TEXT,
    City VARCHAR(100) NOT NULL,
    Province VARCHAR(100) NOT NULL,
    Country VARCHAR(100) DEFAULT 'Zambia',
    PostalCode VARCHAR(20),
    GPSLatitude DECIMAL(10,8),
    GPSLongitude DECIMAL(11,8),
    
    -- Business Details
    BranchType VARCHAR(30) CHECK (BranchType IN ('Main', 'Branch', 'Satellite', 'Kiosk', 'Warehouse')),
    BusinessCategory VARCHAR(50),
    LicenseNumber VARCHAR(100),
    TaxIdentificationNumber VARCHAR(50),
    PharmacyLicenseNumber VARCHAR(100),
    
    -- Operations
    OperatingStatus VARCHAR(20) DEFAULT 'Active' CHECK (OperatingStatus IN ('Active', 'Inactive', 'Under Construction', 'Closed', 'Suspended')),
    OpeningDate DATE,
    SquareFootage INTEGER,
    NumberOfCounters INTEGER DEFAULT 1,
    NumberOfPharmacists INTEGER DEFAULT 1,
    NumberOfCashiers INTEGER DEFAULT 1,
    
    -- Business Hours
    MondayOpen TIME,
    MondayClose TIME,
    TuesdayOpen TIME,
    TuesdayClose TIME,
    WednesdayOpen TIME,
    WednesdayClose TIME,
    ThursdayOpen TIME,
    ThursdayClose TIME,
    FridayOpen TIME,
    FridayClose TIME,
    SaturdayOpen TIME,
    SaturdayClose TIME,
    SundayOpen TIME,
    SundayClose TIME,
    
    -- Financial Information
    BankAccountNumber VARCHAR(50),
    BankName VARCHAR(100),
    BankBranch VARCHAR(100),
    CreditLimit DECIMAL(15,2) DEFAULT 0.00,
    CashWithdrawalLimit DECIMAL(15,2) DEFAULT 0.00,
    
    -- Management
    BranchManagerId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    AssistantManagerId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    
    -- System Integration
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    ParentBranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    
    -- Configuration
    DefaultCurrency VARCHAR(10) DEFAULT 'ZMW',
    TimeZone VARCHAR(50) DEFAULT 'Africa/Lusaka',
    DateFormat VARCHAR(20) DEFAULT 'DD/MM/YYYY',
    
    -- Status and Flags
    IsMainBranch BOOLEAN DEFAULT false,
    IsWarehouse BOOLEAN DEFAULT false,
    Is24Hour BOOLEAN DEFAULT false,
    HasDelivery BOOLEAN DEFAULT false,
    HasPharmacy BOOLEAN DEFAULT true,
    HasPOS BOOLEAN DEFAULT true,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT true
);

-- BranchContacts Table (multiple contacts per branch)
CREATE TABLE IF NOT EXISTS BranchContacts (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    ContactName VARCHAR(100) NOT NULL,
    ContactTitle VARCHAR(50),
    Department VARCHAR(50),
    PhoneNumber VARCHAR(20),
    MobileNumber VARCHAR(20),
    Email VARCHAR(100),
    IsPrimary BOOLEAN DEFAULT false,
    IsManager BOOLEAN DEFAULT false,
    IsOperational BOOLEAN DEFAULT false,
    IsFinancial BOOLEAN DEFAULT false,
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- BranchDepartments Table (departmental structure within branches)
CREATE TABLE IF NOT EXISTS BranchDepartments (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    DepartmentName VARCHAR(100) NOT NULL,
    DepartmentCode VARCHAR(20),
    DepartmentType VARCHAR(30) CHECK (DepartmentType IN ('Pharmacy', 'Retail', 'Warehouse', 'Admin', 'Clinical', 'Customer Service')),
    
    -- Management
    DepartmentHeadId INTEGER REFERENCES UserAccounts(Id) ON DELETE SET NULL,
    
    -- Operations
    OperatingHours TEXT, -- JSON with operating hours
    StaffCount INTEGER DEFAULT 0,
    BudgetLimit DECIMAL(15,2) DEFAULT 0.00,
    
    -- System Fields
    IsActive BOOLEAN DEFAULT true,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Branch Inventory and Stock Tables
-- ========================================

-- BranchInventory Table (inventory at branch level)
CREATE TABLE IF NOT EXISTS BranchInventory (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE CASCADE,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE CASCADE,
    
    -- Stock Information
    CurrentStock INTEGER NOT NULL DEFAULT 0,
    ReservedStock INTEGER NOT NULL DEFAULT 0,
    AvailableStock INTEGER GENERATED ALWAYS AS (CurrentStock - ReservedStock) STORED,
    MinimumStock INTEGER DEFAULT 5,
    MaximumStock INTEGER DEFAULT 1000,
    ReorderLevel INTEGER DEFAULT 10,
    ReorderQuantity INTEGER DEFAULT 50,
    
    -- Location Information
    StorageLocation VARCHAR(100),
    ShelfLocation VARCHAR(50),
    RackLocation VARCHAR(50),
    BinLocation VARCHAR(50),
    
    -- Cost and Pricing
    AverageCost DECIMAL(10,2) DEFAULT 0.00,
    LastCost DECIMAL(10,2) DEFAULT 0.00,
    StandardCost DECIMAL(10,2) DEFAULT 0.00,
    RetailPrice DECIMAL(10,2) DEFAULT 0.00,
    
    -- Movement Tracking
    LastStockUpdate TIMESTAMP WITH TIME ZONE,
    LastStockInDate DATE,
    LastStockOutDate DATE,
    DaysOfSupply INTEGER GENERATED ALWAYS AS (
        CASE 
            WHEN CurrentStock > 0 AND LastStockOutDate IS NOT NULL 
            THEN CURRENT_DATE - LastStockOutDate
            ELSE 999 
        END
    ) STORED,
    
    -- Status
    StockStatus VARCHAR(20) GENERATED ALWAYS AS (
        CASE 
            WHEN CurrentStock = 0 THEN 'Out of Stock'
            WHEN CurrentStock <= ReorderLevel THEN 'Low Stock'
            WHEN CurrentStock >= MaximumStock THEN 'Overstock'
            ELSE 'In Stock'
        END
    ) STORED,
    
    -- System Fields
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(BranchId, ProductId),
    UNIQUE(BranchId, InventoryItemId)
);

-- BranchStockMovements Table (detailed stock movements at branch level)
CREATE TABLE IF NOT EXISTS BranchStockMovements (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE SET NULL,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE SET NULL,
    
    -- Movement Details
    MovementType VARCHAR(30) NOT NULL CHECK (MovementType IN ('Stock In', 'Stock Out', 'Transfer In', 'Transfer Out', 'Adjustment', 'Sale', 'Return', 'Damage', 'Expiry')),
    MovementDate TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ReferenceType VARCHAR(50), -- 'Purchase', 'Sale', 'Transfer', 'Adjustment', etc.
    ReferenceId INTEGER,
    ReferenceNumber VARCHAR(100),
    
    -- Quantity Information
    Quantity INTEGER NOT NULL,
    UnitCost DECIMAL(10,2) DEFAULT 0.00,
    TotalCost DECIMAL(15,2) DEFAULT 0.00,
    
    -- Stock Levels
    PreviousStock INTEGER NOT NULL,
    NewStock INTEGER NOT NULL,
    
    -- Additional Information
    Reason VARCHAR(200),
    BatchNumber VARCHAR(100),
    ExpiryDate DATE,
    StorageLocation VARCHAR(100),
    
    -- User Information
    UserId INTEGER REFERENCES UserAccounts(Id),
    UserName VARCHAR(200),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Inter-Branch Transfer Tables
-- ========================================

-- InterBranchTransfers Table (managing transfers between branches)
CREATE TABLE IF NOT EXISTS InterBranchTransfers (
    Id SERIAL PRIMARY KEY,
    TransferNumber VARCHAR(50) UNIQUE NOT NULL,
    FromBranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE RESTRICT,
    ToBranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE RESTRICT,
    
    -- Transfer Details
    TransferDate DATE NOT NULL DEFAULT CURRENT_DATE,
    ExpectedDeliveryDate DATE,
    ActualDeliveryDate DATE,
    TransferStatus VARCHAR(20) DEFAULT 'Pending' CHECK (TransferStatus IN ('Pending', 'Approved', 'In Transit', 'Partially Received', 'Completed', 'Cancelled', 'Rejected')),
    Priority VARCHAR(20) DEFAULT 'Normal' CHECK (Priority IN ('Low', 'Normal', 'High', 'Urgent')),
    
    -- Financial Information
    TotalValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    Currency VARCHAR(10) DEFAULT 'ZMW',
    ShippingCost DECIMAL(10,2) DEFAULT 0.00,
    InsuranceCost DECIMAL(10,2) DEFAULT 0.00,
    
    -- Transportation Details
    TransportMethod VARCHAR(50), -- 'Company Vehicle', 'Courier', 'Staff', 'Third Party'
    VehicleNumber VARCHAR(50),
    DriverName VARCHAR(100),
    DriverContact VARCHAR(20),
    TrackingNumber VARCHAR(100),
    
    -- Approval Workflow
    RequestedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    RejectedBy INTEGER REFERENCES UserAccounts(Id),
    RejectedAt TIMESTAMP WITH TIME ZONE,
    RejectionReason VARCHAR(500),
    
    -- Delivery Information
    SentBy INTEGER REFERENCES UserAccounts(Id),
    SentAt TIMESTAMP WITH TIME ZONE,
    ReceivedBy INTEGER REFERENCES UserAccounts(Id),
    ReceivedAt TIMESTAMP WITH TIME ZONE,
    
    -- Notes and Attachments
    TransferNotes VARCHAR(1000),
    SpecialInstructions VARCHAR(500),
    AttachmentPath VARCHAR(500),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- InterBranchTransferItems Table (line items for inter-branch transfers)
CREATE TABLE IF NOT EXISTS InterBranchTransferItems (
    Id SERIAL PRIMARY KEY,
    TransferId INTEGER NOT NULL REFERENCES InterBranchTransfers(Id) ON DELETE CASCADE,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE SET NULL,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE SET NULL,
    
    -- Item Details
    ItemDescription VARCHAR(500) NOT NULL,
    ProductCode VARCHAR(100),
    Barcode VARCHAR(100),
    
    -- Quantity Information
    RequestedQuantity INTEGER NOT NULL CHECK (RequestedQuantity > 0),
    SentQuantity INTEGER NOT NULL DEFAULT 0,
    ReceivedQuantity INTEGER NOT NULL DEFAULT 0,
    PendingQuantity INTEGER GENERATED ALWAYS AS (SentQuantity - ReceivedQuantity) STORED,
    
    -- Quality and Condition
    ConditionStatus VARCHAR(20) DEFAULT 'Good' CHECK (ConditionStatus IN ('Good', 'Damaged', 'Expired', 'Short Expiry')),
    QualityChecked BOOLEAN DEFAULT false,
    QualityCheckedBy INTEGER REFERENCES UserAccounts(Id),
    QualityCheckedAt TIMESTAMP WITH TIME ZONE,
    QualityNotes VARCHAR(500),
    
    -- Batch and Expiry
    BatchNumber VARCHAR(100),
    ManufactureDate DATE,
    ExpiryDate DATE,
    
    -- Pricing
    UnitCost DECIMAL(10,2) NOT NULL CHECK (UnitCost >= 0),
    TotalCost DECIMAL(15,2) NOT NULL CHECK (TotalCost >= 0),
    
    -- Discrepancy Information
    HasDiscrepancy BOOLEAN DEFAULT false,
    DiscrepancyType VARCHAR(50), -- 'Shortage', 'Damage', 'Wrong Item', 'Expired'
    DiscrepancyQuantity INTEGER DEFAULT 0,
    DiscrepancyReason VARCHAR(500),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Branch Performance and Analytics Tables
-- ========================================

-- BranchPerformanceMetrics Table (branch performance tracking)
CREATE TABLE IF NOT EXISTS BranchPerformanceMetrics (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Period Information
    MetricPeriod VARCHAR(50) NOT NULL, -- '2024-01', '2024-W01', '2024-Q1'
    MetricType VARCHAR(20) NOT NULL CHECK (MetricType IN ('Daily', 'Weekly', 'Monthly', 'Quarterly', 'Annual')),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    
    -- Sales Metrics
    TotalSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalTransactions INTEGER NOT NULL DEFAULT 0,
    AverageTransactionValue DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    SalesGrowthPercentage DECIMAL(5,2) DEFAULT 0.00,
    
    -- Customer Metrics
    UniqueCustomers INTEGER NOT NULL DEFAULT 0,
    NewCustomers INTEGER NOT NULL DEFAULT 0,
    CustomerRetentionRate DECIMAL(5,2) DEFAULT 0.00,
    
    -- Inventory Metrics
    InventoryTurnover DECIMAL(5,2) DEFAULT 0.00,
    StockAccuracy DECIMAL(5,2) DEFAULT 100.00,
    ShrinkageRate DECIMAL(5,2) DEFAULT 0.00,
    
    -- Operational Metrics
    StaffProductivity DECIMAL(10,2) DEFAULT 0.00, -- sales per staff hour
    OperatingEfficiency DECIMAL(5,2) DEFAULT 100.00,
    ComplianceScore DECIMAL(5,2) DEFAULT 100.00,
    
    -- Financial Metrics
    GrossProfit DECIMAL(15,2) DEFAULT 0.00,
    NetProfit DECIMAL(15,2) DEFAULT 0.00,
    ProfitMargin DECIMAL(5,2) DEFAULT 0.00,
    OperatingExpenses DECIMAL(15,2) DEFAULT 0.00,
    
    -- Ranking and Comparison
    RegionalRank INTEGER,
    NationalRank INTEGER,
    PerformanceGrade VARCHAR(10) CHECK (PerformanceGrade IN ('A+', 'A', 'B', 'C', 'D', 'F')),
    
    -- System Fields
    RecordedBy INTEGER REFERENCES UserAccounts(Id),
    RecordedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- BranchTargets Table (branch-specific targets and goals)
CREATE TABLE IF NOT EXISTS BranchTargets (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Target Period
    TargetPeriod VARCHAR(50) NOT NULL, -- '2024-01', '2024-Q1', '2024'
    TargetType VARCHAR(20) NOT NULL CHECK (TargetType IN ('Daily', 'Weekly', 'Monthly', 'Quarterly', 'Annual')),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    
    -- Sales Targets
    SalesTarget DECIMAL(15,2) NOT NULL CHECK (SalesTarget >= 0),
    TransactionTarget INTEGER NOT NULL DEFAULT 0,
    CustomerTarget INTEGER NOT NULL DEFAULT 0,
    
    -- Operational Targets
    InventoryAccuracyTarget DECIMAL(5,2) DEFAULT 100.00,
    ShrinkageTarget DECIMAL(5,2) DEFAULT 0.00,
    CustomerServiceTarget DECIMAL(3,2) DEFAULT 4.50, -- 1-5 scale
    
    -- Financial Targets
    GrossProfitTarget DECIMAL(15,2) DEFAULT 0.00,
    NetProfitTarget DECIMAL(15,2) DEFAULT 0.00,
    ExpenseTarget DECIMAL(15,2) DEFAULT 0.00,
    
    -- Status and Approval
    TargetStatus VARCHAR(20) DEFAULT 'Active' CHECK (TargetStatus IN ('Active', 'Achieved', 'Missed', 'Cancelled', 'Revised')),
    SetBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Branch Configuration and Settings Tables
-- ========================================

-- BranchSettings Table (branch-specific configurations)
CREATE TABLE IF NOT EXISTS BranchSettings (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    SettingCategory VARCHAR(50) NOT NULL, -- 'POS', 'Inventory', 'Reporting', 'Payments', etc.
    SettingKey VARCHAR(100) NOT NULL,
    SettingValue TEXT,
    SettingType VARCHAR(20) DEFAULT 'String' CHECK (SettingType IN ('String', 'Integer', 'Decimal', 'Boolean', 'JSON')),
    Description VARCHAR(500),
    IsEditable BOOLEAN DEFAULT true,
    RequiresRestart BOOLEAN DEFAULT false,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(BranchId, SettingCategory, SettingKey)
);

-- BranchEquipment Table (equipment and assets at branches)
CREATE TABLE IF NOT EXISTS BranchEquipment (
    Id SERIAL PRIMARY KEY,
    BranchId INTEGER NOT NULL REFERENCES Branches(Id) ON DELETE CASCADE,
    EquipmentType VARCHAR(50) NOT NULL CHECK (EquipmentType IN ('POS Terminal', 'Computer', 'Printer', 'Scanner', 'Scale', 'Camera', 'Server', 'Network', 'Furniture', 'Vehicle')),
    EquipmentName VARCHAR(200) NOT NULL,
    EquipmentCode VARCHAR(50) UNIQUE,
    SerialNumber VARCHAR(100),
    Model VARCHAR(100),
    Brand VARCHAR(100),
    
    -- Purchase Information
    PurchaseDate DATE,
    PurchaseCost DECIMAL(10,2),
    SupplierId INTEGER REFERENCES Suppliers(Id),
    WarrantyExpiry DATE,
    
    -- Location and Status
    Location VARCHAR(100),
    Status VARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Inactive', 'Under Maintenance', 'Damaged', 'Lost', 'Disposed')),
    LastMaintenanceDate DATE,
    NextMaintenanceDate DATE,
    
    -- Usage Information
    UsageHours INTEGER DEFAULT 0,
    LastUsedDate DATE,
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Branch Management Tables
-- ========================================

-- Branches Indexes
CREATE INDEX IF NOT EXISTS idx_branches_code ON Branches(BranchCode);
CREATE INDEX IF NOT EXISTS idx_branches_name ON Branches(BranchName);
CREATE INDEX IF NOT EXISTS idx_branches_type ON Branches(BranchType);
CREATE INDEX IF NOT EXISTS idx_branches_city ON Branches(City);
CREATE INDEX IF NOT EXISTS idx_branches_province ON Branches(Province);
CREATE INDEX IF NOT EXISTS idx_branches_tenant ON Branches(TenantId);
CREATE INDEX IF NOT EXISTS idx_branches_parent ON Branches(ParentBranchId);
CREATE INDEX IF NOT EXISTS idx_branches_manager ON Branches(BranchManagerId);
CREATE INDEX IF NOT EXISTS idx_branches_status ON Branches(OperatingStatus);
CREATE INDEX IF NOT EXISTS idx_branches_active ON Branches(IsActive);

-- BranchContacts Indexes
CREATE INDEX IF NOT EXISTS idx_branchcontacts_branch ON BranchContacts(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchcontacts_primary ON BranchContacts(IsPrimary);
CREATE INDEX IF NOT EXISTS idx_branchcontacts_manager ON BranchContacts(IsManager);

-- BranchDepartments Indexes
CREATE INDEX IF NOT EXISTS idx_branchdepartments_branch ON BranchDepartments(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchdepartments_head ON BranchDepartments(DepartmentHeadId);
CREATE INDEX IF NOT EXISTS idx_branchdepartments_type ON BranchDepartments(DepartmentType);

-- BranchInventory Indexes
CREATE INDEX IF NOT EXISTS idx_branchinventory_branch ON BranchInventory(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchinventory_product ON BranchInventory(ProductId);
CREATE INDEX IF NOT EXISTS idx_branchinventory_inventory ON BranchInventory(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_branchinventory_status ON BranchInventory(StockStatus);
CREATE INDEX IF NOT EXISTS idx_branchinventory_available ON BranchInventory(AvailableStock);

-- BranchStockMovements Indexes
CREATE INDEX IF NOT EXISTS idx_branchstockmovements_branch ON BranchStockMovements(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchstockmovements_product ON BranchStockMovements(ProductId);
CREATE INDEX IF NOT EXISTS idx_branchstockmovements_inventory ON BranchStockMovements(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_branchstockmovements_type ON BranchStockMovements(MovementType);
CREATE INDEX IF NOT EXISTS idx_branchstockmovements_date ON BranchStockMovements(MovementDate);

-- InterBranchTransfers Indexes
CREATE INDEX IF NOT EXISTS idx_interbranchtransfers_number ON InterBranchTransfers(TransferNumber);
CREATE INDEX IF NOT EXISTS idx_interbranchtransfers_from ON InterBranchTransfers(FromBranchId);
CREATE INDEX IF NOT EXISTS idx_interbranchtransfers_to ON InterBranchTransfers(ToBranchId);
CREATE INDEX IF NOT EXISTS idx_interbranchtransfers_status ON InterBranchTransfers(TransferStatus);
CREATE INDEX IF NOT EXISTS idx_interbranchtransfers_date ON InterBranchTransfers(TransferDate);

-- InterBranchTransferItems Indexes
CREATE INDEX IF NOT EXISTS idx_interbranchtransferitems_transfer ON InterBranchTransferItems(TransferId);
CREATE INDEX IF NOT EXISTS idx_interbranchtransferitems_product ON InterBranchTransferItems(ProductId);
CREATE INDEX IF NOT EXISTS idx_interbranchtransferitems_inventory ON InterBranchTransferItems(InventoryItemId);

-- BranchPerformanceMetrics Indexes
CREATE INDEX IF NOT EXISTS idx_branchperformancemetrics_branch ON BranchPerformanceMetrics(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchperformancemetrics_tenant ON BranchPerformanceMetrics(TenantId);
CREATE INDEX IF NOT EXISTS idx_branchperformancemetrics_period ON BranchPerformanceMetrics(MetricPeriod);
CREATE INDEX IF NOT EXISTS idx_branchperformancemetrics_type ON BranchPerformanceMetrics(MetricType);

-- BranchTargets Indexes
CREATE INDEX IF NOT EXISTS idx_branchtargets_branch ON BranchTargets(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchtargets_tenant ON BranchTargets(TenantId);
CREATE INDEX IF NOT EXISTS idx_branchtargets_period ON BranchTargets(TargetPeriod);
CREATE INDEX IF NOT EXISTS idx_branchtargets_type ON BranchTargets(TargetType);

-- BranchSettings Indexes
CREATE INDEX IF NOT EXISTS idx_branchsettings_branch ON BranchSettings(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchsettings_category ON BranchSettings(SettingCategory);

-- BranchEquipment Indexes
CREATE INDEX IF NOT EXISTS idx_branchequipment_branch ON BranchEquipment(BranchId);
CREATE INDEX IF NOT EXISTS idx_branchequipment_type ON BranchEquipment(EquipmentType);
CREATE INDEX IF NOT EXISTS idx_branchequipment_status ON BranchEquipment(Status);

-- ========================================
-- Triggers for Branch Management Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to branch tables
CREATE TRIGGER update_branches_updated_at BEFORE UPDATE ON Branches 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branchcontacts_updated_at BEFORE UPDATE ON BranchContacts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branchdepartments_updated_at BEFORE UPDATE ON BranchDepartments 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branchinventory_updated_at BEFORE UPDATE ON BranchInventory 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_interbranchtransfers_updated_at BEFORE UPDATE ON InterBranchTransfers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_interbranchtransferitems_updated_at BEFORE UPDATE ON InterBranchTransferItems 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branchtargets_updated_at BEFORE UPDATE ON BranchTargets 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branchsettings_updated_at BEFORE UPDATE ON BranchSettings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branchequipment_updated_at BEFORE UPDATE ON BranchEquipment 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to generate transfer numbers
CREATE OR REPLACE FUNCTION generate_transfer_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.TransferNumber := 'TRF-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP)::text, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_transfer_number_trigger
    BEFORE INSERT ON InterBranchTransfers
    FOR EACH ROW EXECUTE FUNCTION generate_transfer_number();

-- ========================================
-- Views for Branch Management
-- ========================================

-- View for Branch Summary
CREATE OR REPLACE VIEW BranchSummary AS
SELECT 
    b.Id,
    b.BranchCode,
    b.BranchName,
    b.BranchType,
    b.City,
    b.Province,
    b.OperatingStatus,
    b.BranchManagerId,
    u.FirstName || ' ' || u.LastName as BranchManagerName,
    b.TenantId,
    t.BusinessName as TenantName,
    b.OpeningDate,
    b.IsActive,
    COUNT(DISTINCT bd.Id) as DepartmentCount,
    COUNT(DISTINCT be.Id) as EquipmentCount,
    COUNT(DISTINCT bi.Id) as InventoryItemCount
FROM Branches b
LEFT JOIN UserAccounts u ON b.BranchManagerId = u.Id
LEFT JOIN Tenants t ON b.TenantId = t.Id
LEFT JOIN BranchDepartments bd ON b.Id = bd.BranchId AND bd.IsActive = true
LEFT JOIN BranchEquipment be ON b.Id = be.BranchId AND be.Status = 'Active'
LEFT JOIN BranchInventory bi ON b.Id = bi.BranchId AND bi.IsActive = true
GROUP BY b.Id, b.BranchCode, b.BranchName, b.BranchType, b.City, b.Province, 
         b.OperatingStatus, b.BranchManagerId, u.FirstName, u.LastName, 
         b.TenantId, t.BusinessName, b.OpeningDate, b.IsActive;

-- View for Branch Inventory Status
CREATE OR REPLACE VIEW BranchInventoryStatus AS
SELECT 
    b.Id as BranchId,
    b.BranchName,
    COUNT(DISTINCT bi.ProductId) as TotalProducts,
    COUNT(DISTINCT CASE WHEN bi.CurrentStock = 0 THEN bi.ProductId END) as OutOfStockProducts,
    COUNT(DISTINCT CASE WHEN bi.CurrentStock <= bi.ReorderLevel THEN bi.ProductId END) as LowStockProducts,
    COUNT(DISTINCT CASE WHEN bi.CurrentStock > 0 THEN bi.ProductId END) as InStockProducts,
    SUM(bi.CurrentStock * bi.AverageCost) as TotalInventoryValue,
    AVG(bi.DaysOfSupply) as AverageDaysOfSupply
FROM Branches b
LEFT JOIN BranchInventory bi ON b.Id = bi.BranchId AND bi.IsActive = true
WHERE b.IsActive = true
GROUP BY b.Id, b.BranchName;

-- View for Inter-Branch Transfer Status
CREATE OR REPLACE VIEW InterBranchTransferStatus AS
SELECT 
    ibt.Id,
    ibt.TransferNumber,
    fb.BranchName as FromBranch,
    tb.BranchName as ToBranch,
    ibt.TransferDate,
    ibt.ExpectedDeliveryDate,
    ibt.ActualDeliveryDate,
    ibt.TransferStatus,
    ibt.TotalValue,
    ibt.Priority,
    COUNT(DISTINCT ibti.ProductId) as ItemCount,
    SUM(ibti.SentQuantity) as TotalQuantity,
    SUM(ibti.ReceivedQuantity) as TotalReceived,
    req.FirstName || ' ' || req.LastName as RequestedByName,
    app.FirstName || ' ' || app.LastName as ApprovedByName,
    ibt.CreatedAt
FROM InterBranchTransfers ibt
JOIN Branches fb ON ibt.FromBranchId = fb.Id
JOIN Branches tb ON ibt.ToBranchId = tb.Id
LEFT JOIN UserAccounts req ON ibt.RequestedBy = req.Id
LEFT JOIN UserAccounts app ON ibt.ApprovedBy = app.Id
LEFT JOIN InterBranchTransferItems ibti ON ibt.Id = ibti.TransferId
GROUP BY ibt.Id, ibt.TransferNumber, fb.BranchName, tb.BranchName, 
         ibt.TransferDate, ibt.ExpectedDeliveryDate, ibt.ActualDeliveryDate, 
         ibt.TransferStatus, ibt.TotalValue, ibt.Priority, 
         req.FirstName, req.LastName, app.FirstName, app.LastName, ibt.CreatedAt
ORDER BY ibt.TransferDate DESC;

-- View for Branch Performance Dashboard
CREATE OR REPLACE VIEW BranchPerformanceDashboard AS
SELECT 
    b.Id as BranchId,
    b.BranchName,
    b.City,
    bpm.TotalSales,
    bpm.TotalTransactions,
    bpm.AverageTransactionValue,
    bpm.UniqueCustomers,
    bpm.InventoryTurnover,
    bpm.StockAccuracy,
    bpm.ShrinkageRate,
    bpm.StaffProductivity,
    bpm.PerformanceGrade,
    bt.SalesTarget,
    CASE 
        WHEN bt.SalesTarget > 0 THEN ROUND((bpm.TotalSales / bt.SalesTarget) * 100, 2)
        ELSE 0
    END as TargetAchievementPercentage,
    t.BusinessName as TenantName
FROM Branches b
LEFT JOIN BranchPerformanceMetrics bpm ON b.Id = bpm.BranchId 
    AND bpm.MetricPeriod = TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'YYYY-MM')
    AND bpm.MetricType = 'Monthly'
LEFT JOIN BranchTargets bt ON b.Id = bt.BranchId 
    AND bt.TargetPeriod = TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'YYYY-MM')
    AND bt.TargetType = 'Monthly'
LEFT JOIN Tenants t ON b.TenantId = t.Id
WHERE b.IsActive = true
ORDER BY bpm.TotalSales DESC;
