-- Supplier Management Tables
-- This file contains SQL for supplier management, procurement, and vendor relationships

-- ========================================
-- Supplier Master Tables
-- ========================================

-- Suppliers Table (comprehensive supplier information)
CREATE TABLE IF NOT EXISTS Suppliers (
    Id SERIAL PRIMARY KEY,
    SupplierCode VARCHAR(50) UNIQUE NOT NULL,
    BusinessName VARCHAR(200) NOT NULL,
    TradeName VARCHAR(200),
    RegistrationNumber VARCHAR(100),
    TaxIdentificationNumber VARCHAR(50),
    PharmacyLicenseNumber VARCHAR(100),
    DrugSupplierLicense VARCHAR(100),
    
    -- Contact Information
    ContactPerson VARCHAR(100),
    ContactPersonTitle VARCHAR(50),
    PrimaryPhoneNumber VARCHAR(20),
    SecondaryPhoneNumber VARCHAR(20),
    Email VARCHAR(100),
    AlternativeEmail VARCHAR(100),
    Website VARCHAR(200),
    
    -- Address Information
    PhysicalAddress TEXT,
    PostalAddress TEXT,
    City VARCHAR(100),
    Province VARCHAR(100),
    Country VARCHAR(100) DEFAULT 'Zambia',
    PostalCode VARCHAR(20),
    
    -- Business Details
    BusinessType VARCHAR(50) CHECK (BusinessType IN ('Manufacturer', 'Wholesaler', 'Distributor', 'Importer', 'Local Supplier')),
    Industry VARCHAR(100),
    YearsInOperation INTEGER,
    NumberOfEmployees INTEGER,
    AnnualRevenue DECIMAL(15,2),
    
    -- Banking Information
    BankName VARCHAR(100),
    BankAccountNumber VARCHAR(50),
    BankAccountName VARCHAR(100),
    BankBranch VARCHAR(100),
    BankCode VARCHAR(20),
    SwiftCode VARCHAR(20),
    
    -- Payment Terms
    PaymentTerms VARCHAR(50) DEFAULT 'Net 30',
    CreditLimit DECIMAL(15,2) DEFAULT 0.00,
    CreditPeriod INTEGER DEFAULT 30, -- in days
    DiscountTerms VARCHAR(100),
    EarlyPaymentDiscount DECIMAL(5,2) DEFAULT 0.00,
    
    -- Supplier Classification
    SupplierCategory VARCHAR(50) CHECK (SupplierCategory IN ('Pharmaceuticals', 'Medical Devices', 'Consumables', 'Equipment', 'General')),
    SupplierStatus VARCHAR(20) DEFAULT 'Active' CHECK (SupplierStatus IN ('Active', 'Inactive', 'Suspended', 'Blacklisted', 'Under Review')),
    PriorityLevel VARCHAR(20) DEFAULT 'Medium' CHECK (PriorityLevel IN ('High', 'Medium', 'Low')),
    IsPreferred BOOLEAN DEFAULT false,
    IsBlacklisted BOOLEAN DEFAULT false,
    BlacklistReason VARCHAR(500),
    
    -- Performance Metrics
    OnTimeDeliveryRate DECIMAL(5,2) DEFAULT 0.00, -- percentage
    QualityRating DECIMAL(3,2) DEFAULT 0.00, -- 1-5 scale
    PriceCompetitiveness DECIMAL(3,2) DEFAULT 0.00, -- 1-5 scale
    OverallRating DECIMAL(3,2) DEFAULT 0.00, -- 1-5 scale
    LastPerformanceReview DATE,
    
    -- Compliance and Certifications
    ZambianRegistered BOOLEAN DEFAULT false,
    GmpCertified BOOLEAN DEFAULT false,
    IsoCertified BOOLEAN DEFAULT false,
    CertificationExpiryDate DATE,
    RegulatoryComplianceStatus VARCHAR(20) DEFAULT 'Pending',
    
    -- System Fields
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT true
);

-- Supplier Contacts Table (multiple contacts per supplier)
CREATE TABLE IF NOT EXISTS SupplierContacts (
    Id SERIAL PRIMARY KEY,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE CASCADE,
    ContactName VARCHAR(100) NOT NULL,
    ContactTitle VARCHAR(50),
    Department VARCHAR(50),
    PhoneNumber VARCHAR(20),
    MobileNumber VARCHAR(20),
    Email VARCHAR(100),
    IsPrimary BOOLEAN DEFAULT false,
    IsOrderContact BOOLEAN DEFAULT false,
    IsBillingContact BOOLEAN DEFAULT false,
    IsTechnicalContact BOOLEAN DEFAULT false,
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Supplier Products Table (products supplied by each supplier)
CREATE TABLE IF NOT EXISTS SupplierProducts (
    Id SERIAL PRIMARY KEY,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE CASCADE,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE CASCADE,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE CASCADE,
    SupplierProductCode VARCHAR(100),
    SupplierProductName VARCHAR(200),
    Description VARCHAR(500),
    
    -- Pricing Information
    UnitCost DECIMAL(10,2) NOT NULL CHECK (UnitCost >= 0),
    Currency VARCHAR(10) DEFAULT 'ZMW',
    MinimumOrderQuantity INTEGER DEFAULT 1,
    MaximumOrderQuantity INTEGER,
    OrderMultiples INTEGER DEFAULT 1,
    
    -- Availability and Lead Time
    IsAvailable BOOLEAN DEFAULT true,
    LeadTimeDays INTEGER DEFAULT 7,
    MinimumOrderValue DECIMAL(10,2) DEFAULT 0.00,
    
    -- Quality and Compliance
    QualityGrade VARCHAR(20),
    BatchNumber VARCHAR(100),
    ManufactureDate DATE,
    ExpiryDate DATE,
    StorageRequirements VARCHAR(200),
    
    -- Supplier-Specific Details
    SupplierCatalogNumber VARCHAR(100),
    SupplierBarcode VARCHAR(100),
    PackagingInformation VARCHAR(200),
    WeightPerUnit DECIMAL(10,3),
    Dimensions VARCHAR(50),
    
    -- System Fields
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(SupplierId, ProductId),
    UNIQUE(SupplierId, InventoryItemId)
);

-- ========================================
-- Procurement and Purchase Orders Tables
-- ========================================

-- PurchaseOrders Table (main purchase order management)
CREATE TABLE IF NOT EXISTS PurchaseOrders (
    Id SERIAL PRIMARY KEY,
    PurchaseOrderNumber VARCHAR(50) UNIQUE NOT NULL,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE RESTRICT,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    BranchId INTEGER REFERENCES Branches(Id) ON DELETE SET NULL,
    
    -- Order Details
    OrderDate DATE NOT NULL DEFAULT CURRENT_DATE,
    ExpectedDeliveryDate DATE,
    ActualDeliveryDate DATE,
    OrderStatus VARCHAR(20) DEFAULT 'Draft' CHECK (OrderStatus IN ('Draft', 'Sent', 'Confirmed', 'PartiallyReceived', 'Completed', 'Cancelled', 'On Hold')),
    Priority VARCHAR(20) DEFAULT 'Normal' CHECK (Priority IN ('Low', 'Normal', 'High', 'Urgent')),
    
    -- Financial Information
    Subtotal DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TaxAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    DiscountAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    ShippingCost DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    Currency VARCHAR(10) DEFAULT 'ZMW',
    
    -- Payment Terms
    PaymentTerms VARCHAR(50),
    DueDate DATE,
    PaymentStatus VARCHAR(20) DEFAULT 'Pending' CHECK (PaymentStatus IN ('Pending', 'Partial', 'Paid', 'Overdue')),
    PaidAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    
    -- Delivery Information
    DeliveryAddress TEXT,
    DeliveryInstructions VARCHAR(500),
    DeliveryContact VARCHAR(100),
    DeliveryPhoneNumber VARCHAR(20),
    
    -- Approval Workflow
    RequestedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedBy INTEGER REFERENCES UserAccounts(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    ApprovalComments VARCHAR(500),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- PurchaseOrderItems Table (line items for purchase orders)
CREATE TABLE IF NOT EXISTS PurchaseOrderItems (
    Id SERIAL PRIMARY KEY,
    PurchaseOrderId INTEGER NOT NULL REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE SET NULL,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE SET NULL,
    SupplierProductId INTEGER REFERENCES SupplierProducts(Id) ON DELETE SET NULL,
    
    -- Item Details
    ItemDescription VARCHAR(500) NOT NULL,
    SupplierProductCode VARCHAR(100),
    BatchNumber VARCHAR(100),
    ManufactureDate DATE,
    ExpiryDate DATE,
    
    -- Quantity and Pricing
    OrderQuantity INTEGER NOT NULL CHECK (OrderQuantity > 0),
    ReceivedQuantity INTEGER NOT NULL DEFAULT 0,
    PendingQuantity INTEGER GENERATED ALWAYS AS (OrderQuantity - ReceivedQuantity) STORED,
    UnitCost DECIMAL(10,2) NOT NULL CHECK (UnitCost >= 0),
    DiscountPercentage DECIMAL(5,2) DEFAULT 0.00,
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    TaxRate DECIMAL(5,2) DEFAULT 0.00,
    TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    LineTotal DECIMAL(10,2) NOT NULL CHECK (LineTotal >= 0),
    
    -- Quality Control
    QualityStatus VARCHAR(20) DEFAULT 'Pending' CHECK (QualityStatus IN ('Pending', 'Passed', 'Failed', 'Requires Retest')),
    QualityCheckedBy INTEGER REFERENCES UserAccounts(Id),
    QualityCheckedAt TIMESTAMP WITH TIME ZONE,
    QualityNotes VARCHAR(500),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- PurchaseOrderReceipts Table (goods receipt tracking)
CREATE TABLE IF NOT EXISTS PurchaseOrderReceipts (
    Id SERIAL PRIMARY KEY,
    PurchaseOrderId INTEGER NOT NULL REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
    ReceiptNumber VARCHAR(50) UNIQUE NOT NULL,
    ReceiptDate DATE NOT NULL DEFAULT CURRENT_DATE,
    
    -- Receipt Details
    ReceivedBy INTEGER REFERENCES UserAccounts(Id),
    DeliveryNoteNumber VARCHAR(100),
    VehicleNumber VARCHAR(50),
    DriverName VARCHAR(100),
    DriverContact VARCHAR(20),
    
    -- Quality Control
    QualityChecked BOOLEAN DEFAULT false,
    QualityCheckedBy INTEGER REFERENCES UserAccounts(Id),
    QualityCheckedAt TIMESTAMP WITH TIME ZONE,
    QualityStatus VARCHAR(20) DEFAULT 'Pending',
    QualityNotes VARCHAR(500),
    
    -- Discrepancy Information
    HasDiscrepancies BOOLEAN DEFAULT false,
    DiscrepancyType VARCHAR(50), -- 'Shortage', 'Damage', 'Wrong Item', 'Expired'
    DiscrepancyDescription VARCHAR(500),
    DiscrepancyResolved BOOLEAN DEFAULT false,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- PurchaseOrderReceiptItems Table (detailed receipt line items)
CREATE TABLE IF NOT EXISTS PurchaseOrderReceiptItems (
    Id SERIAL PRIMARY KEY,
    PurchaseOrderReceiptId INTEGER NOT NULL REFERENCES PurchaseOrderReceipts(Id) ON DELETE CASCADE,
    PurchaseOrderItemId INTEGER NOT NULL REFERENCES PurchaseOrderItems(Id) ON DELETE RESTRICT,
    
    -- Receipt Details
    ReceivedQuantity INTEGER NOT NULL CHECK (ReceivedQuantity > 0),
    AcceptedQuantity INTEGER NOT NULL CHECK (AcceptedQuantity >= 0),
    RejectedQuantity INTEGER GENERATED ALWAYS AS (ReceivedQuantity - AcceptedQuantity) STORED,
    
    -- Batch and Expiry Information
    ReceivedBatchNumber VARCHAR(100),
    ReceivedManufactureDate DATE,
    ReceivedExpiryDate DATE,
    
    -- Quality Information
    RejectionReason VARCHAR(200),
    QualityGrade VARCHAR(20),
    StorageLocation VARCHAR(100),
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Supplier Performance and Evaluation Tables
-- ========================================

-- SupplierPerformanceReviews Table (supplier evaluation records)
CREATE TABLE IF NOT EXISTS SupplierPerformanceReviews (
    Id SERIAL PRIMARY KEY,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    ReviewPeriod VARCHAR(50) NOT NULL, -- e.g., '2024-Q1', '2024-01'
    ReviewDate DATE NOT NULL,
    ReviewedBy INTEGER REFERENCES UserAccounts(Id),
    
    -- Performance Metrics
    OnTimeDeliveryScore DECIMAL(5,2) CHECK (OnTimeDeliveryScore >= 0 AND OnTimeDeliveryScore <= 100),
    QualityScore DECIMAL(3,2) CHECK (QualityScore >= 1 AND QualityScore <= 5),
    PriceCompetitivenessScore DECIMAL(3,2) CHECK (PriceCompetitivenessScore >= 1 AND PriceCompetitivenessScore <= 5),
    CommunicationScore DECIMAL(3,2) CHECK (CommunicationScore >= 1 AND CommunicationScore <= 5),
    FlexibilityScore DECIMAL(3,2) CHECK (FlexibilityScore >= 1 AND FlexibilityScore <= 5),
    
    -- Overall Assessment
    OverallScore DECIMAL(3,2) CHECK (OverallScore >= 1 AND OverallScore <= 5),
    PerformanceGrade VARCHAR(10) CHECK (PerformanceGrade IN ('A', 'B', 'C', 'D', 'F')),
    
    -- Review Details
    Strengths TEXT,
    Weaknesses TEXT,
    Opportunities TEXT,
    Threats TEXT,
    ActionPlan TEXT,
    FollowUpRequired BOOLEAN DEFAULT false,
    FollowUpDate DATE,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- SupplierIncidents Table (supplier performance issues)
CREATE TABLE IF NOT EXISTS SupplierIncidents (
    Id SERIAL PRIMARY KEY,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    PurchaseOrderId INTEGER REFERENCES PurchaseOrders(Id) ON DELETE SET NULL,
    
    -- Incident Details
    IncidentDate DATE NOT NULL,
    IncidentType VARCHAR(50) NOT NULL CHECK (IncidentType IN ('Late Delivery', 'Quality Issue', 'Short Supply', 'Wrong Product', 'Documentation Issue', 'Communication Failure')),
    Severity VARCHAR(20) NOT NULL CHECK (Severity IN ('Low', 'Medium', 'High', 'Critical')),
    Description TEXT NOT NULL,
    
    -- Impact Assessment
    FinancialImpact DECIMAL(15,2) DEFAULT 0.00,
    OperationalImpact VARCHAR(200),
    CustomerImpact VARCHAR(200),
    
    -- Resolution Information
    ResolutionStatus VARCHAR(20) DEFAULT 'Open' CHECK (ResolutionStatus IN ('Open', 'In Progress', 'Resolved', 'Escalated')),
    ResolutionDate DATE,
    ResolutionDetails TEXT,
    CorrectiveActions TEXT,
    PreventiveActions TEXT,
    
    -- Follow-up
    FollowUpRequired BOOLEAN DEFAULT false,
    FollowUpDate DATE,
    FollowUpCompleted BOOLEAN DEFAULT false,
    
    -- System Fields
    ReportedBy INTEGER REFERENCES UserAccounts(Id),
    AssignedTo INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Supplier Contracts and Agreements Tables
-- ========================================

-- SupplierContracts Table (contract management)
CREATE TABLE IF NOT EXISTS SupplierContracts (
    Id SERIAL PRIMARY KEY,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    ContractNumber VARCHAR(100) UNIQUE NOT NULL,
    ContractType VARCHAR(50) NOT NULL CHECK (ContractType IN ('Supply Agreement', 'Framework Agreement', 'Service Level Agreement', 'Memorandum of Understanding')),
    
    -- Contract Period
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    AutoRenew BOOLEAN DEFAULT false,
    RenewalNoticePeriod INTEGER DEFAULT 30, -- days
    TrialPeriodEndDate DATE,
    
    -- Financial Terms
    ContractValue DECIMAL(15,2),
    Currency VARCHAR(10) DEFAULT 'ZMW',
    PaymentTerms VARCHAR(50),
    PriceAdjustmentClause VARCHAR(200),
    VolumeDiscounts TEXT, -- JSON with discount tiers
    
    -- Scope and Deliverables
    ProductCategories TEXT, -- JSON array of categories
    SpecificProducts TEXT, -- JSON array of product IDs
    ServiceLevelAgreement TEXT,
    ExclusivityClause BOOLEAN DEFAULT false,
    TerritoryRestrictions TEXT,
    
    -- Performance Requirements
    DeliveryTimeframe VARCHAR(100),
    QualityStandards VARCHAR(200),
    PenaltiesForBreach TEXT,
    BonusForPerformance TEXT,
    
    -- Contract Management
    ContractStatus VARCHAR(20) DEFAULT 'Draft' CHECK (ContractStatus IN ('Draft', 'Under Review', 'Active', 'Expired', 'Terminated', 'Suspended')),
    SignedBy INTEGER REFERENCES UserAccounts(Id),
    SignedDate DATE,
    CounterSignedBy INTEGER REFERENCES UserAccounts(Id),
    CounterSignedDate DATE,
    
    -- Document Management
    ContractDocumentPath VARCHAR(500),
    AmendmentsDocumentPath VARCHAR(500),
    SupportingDocuments TEXT, -- JSON with document paths
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Supplier Pricing and Catalog Tables
-- ========================================

-- SupplierPriceLists Table (price list management)
CREATE TABLE IF NOT EXISTS SupplierPriceLists (
    Id SERIAL PRIMARY KEY,
    SupplierId INTEGER NOT NULL REFERENCES Suppliers(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    PriceListName VARCHAR(100) NOT NULL,
    PriceListType VARCHAR(20) DEFAULT 'Standard' CHECK (PriceListType IN ('Standard', 'Promotional', 'Contract', 'Emergency')),
    
    -- Validity Period
    EffectiveDate DATE NOT NULL DEFAULT CURRENT_DATE,
    ExpiryDate DATE,
    
    -- Pricing Details
    Currency VARCHAR(10) DEFAULT 'ZMW',
    PriceBasis VARCHAR(20) DEFAULT 'Unit' CHECK (PriceBasis IN ('Unit', 'Box', 'Pack', 'Carton')),
    IncludesVAT BOOLEAN DEFAULT false,
    VATRate DECIMAL(5,2) DEFAULT 16.00,
    
    -- Terms and Conditions
    MinimumOrderValue DECIMAL(10,2) DEFAULT 0.00,
    PaymentTerms VARCHAR(50),
    DeliveryTerms VARCHAR(100),
    
    -- Status
    Status VARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Inactive', 'Pending', 'Expired')),
    
    -- System Fields
    CreatedBy INTEGER REFERENCES UserAccounts(Id),
    UpdatedBy INTEGER REFERENCES UserAccounts(Id),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- SupplierPriceListItems Table (detailed price list items)
CREATE TABLE IF NOT EXISTS SupplierPriceListItems (
    Id SERIAL PRIMARY KEY,
    PriceListId INTEGER NOT NULL REFERENCES SupplierPriceLists(Id) ON DELETE CASCADE,
    ProductId INTEGER REFERENCES Products(Id) ON DELETE SET NULL,
    InventoryItemId INTEGER REFERENCES InventoryItems(Id) ON DELETE SET NULL,
    
    -- Product Identification
    SupplierProductCode VARCHAR(100),
    ProductDescription VARCHAR(500),
    Barcode VARCHAR(100),
    
    -- Pricing Information
    UnitPrice DECIMAL(10,2) NOT NULL CHECK (UnitPrice >= 0),
    ListPrice DECIMAL(10,2),
    DiscountPercentage DECIMAL(5,2) DEFAULT 0.00,
    PromotionalPrice DECIMAL(10,2),
    
    -- Quantity Breaks
    QuantityBreak1 INTEGER,
    PriceBreak1 DECIMAL(10,2),
    QuantityBreak2 INTEGER,
    PriceBreak2 DECIMAL(10,2),
    QuantityBreak3 INTEGER,
    PriceBreak3 DECIMAL(10,2),
    
    -- Product Details
    UnitOfMeasure VARCHAR(20),
    PackageSize VARCHAR(50),
    MinimumOrderQuantity INTEGER DEFAULT 1,
    
    -- Availability
    IsAvailable BOOLEAN DEFAULT true,
    LeadTimeDays INTEGER DEFAULT 7,
    
    -- System Fields
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Supplier Management Tables
-- ========================================

-- Suppliers Indexes
CREATE INDEX IF NOT EXISTS idx_suppliers_code ON Suppliers(SupplierCode);
CREATE INDEX IF NOT EXISTS idx_suppliers_name ON Suppliers(BusinessName);
CREATE INDEX IF NOT EXISTS idx_suppliers_registration ON Suppliers(RegistrationNumber);
CREATE INDEX IF NOT EXISTS idx_suppliers_type ON Suppliers(BusinessType);
CREATE INDEX IF NOT EXISTS idx_suppliers_category ON Suppliers(SupplierCategory);
CREATE INDEX IF NOT EXISTS idx_suppliers_status ON Suppliers(SupplierStatus);
CREATE INDEX IF NOT EXISTS idx_suppliers_tenant ON Suppliers(TenantId);
CREATE INDEX IF NOT EXISTS idx_suppliers_active ON Suppliers(IsActive);
CREATE INDEX IF NOT EXISTS idx_suppliers_preferred ON Suppliers(IsPreferred);
CREATE INDEX IF NOT EXISTS idx_suppliers_rating ON Suppliers(OverallRating);

-- Supplier Contacts Indexes
CREATE INDEX IF NOT EXISTS idx_suppliercontacts_supplier ON SupplierContacts(SupplierId);
CREATE INDEX IF NOT EXISTS idx_suppliercontacts_primary ON SupplierContacts(IsPrimary);
CREATE INDEX IF NOT EXISTS idx_suppliercontacts_order ON SupplierContacts(IsOrderContact);

-- Supplier Products Indexes
CREATE INDEX IF NOT EXISTS idx_supplierproducts_supplier ON SupplierProducts(SupplierId);
CREATE INDEX IF NOT EXISTS idx_supplierproducts_product ON SupplierProducts(ProductId);
CREATE INDEX IF NOT EXISTS idx_supplierproducts_inventory ON SupplierProducts(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_supplierproducts_available ON SupplierProducts(IsAvailable);
CREATE INDEX IF NOT EXISTS idx_supplierproducts_active ON SupplierProducts(IsActive);

-- Purchase Orders Indexes
CREATE INDEX IF NOT EXISTS idx_purchaseorders_number ON PurchaseOrders(PurchaseOrderNumber);
CREATE INDEX IF NOT EXISTS idx_purchaseorders_supplier ON PurchaseOrders(SupplierId);
CREATE INDEX IF NOT EXISTS idx_purchaseorders_tenant ON PurchaseOrders(TenantId);
CREATE INDEX IF NOT EXISTS idx_purchaseorders_branch ON PurchaseOrders(BranchId);
CREATE INDEX IF NOT EXISTS idx_purchaseorders_status ON PurchaseOrders(OrderStatus);
CREATE INDEX IF NOT EXISTS idx_purchaseorders_date ON PurchaseOrders(OrderDate);
CREATE INDEX IF NOT EXISTS idx_purchaseorders_payment ON PurchaseOrders(PaymentStatus);

-- Purchase Order Items Indexes
CREATE INDEX IF NOT EXISTS idx_purchaseorderitems_order ON PurchaseOrderItems(PurchaseOrderId);
CREATE INDEX IF NOT EXISTS idx_purchaseorderitems_product ON PurchaseOrderItems(ProductId);
CREATE INDEX IF NOT EXISTS idx_purchaseorderitems_inventory ON PurchaseOrderItems(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_purchaseorderitems_quality ON PurchaseOrderItems(QualityStatus);

-- Purchase Order Receipts Indexes
CREATE INDEX IF NOT EXISTS idx_purchaseorderreceipts_order ON PurchaseOrderReceipts(PurchaseOrderId);
CREATE INDEX IF NOT EXISTS idx_purchaseorderreceipts_number ON PurchaseOrderReceipts(ReceiptNumber);
CREATE INDEX IF NOT EXISTS idx_purchaseorderreceipts_date ON PurchaseOrderReceipts(ReceiptDate);

-- Purchase Order Receipt Items Indexes
CREATE INDEX IF NOT EXISTS idx_purchaseorderreceiptitems_receipt ON PurchaseOrderReceiptItems(PurchaseOrderReceiptId);
CREATE INDEX IF NOT EXISTS idx_purchaseorderreceiptitems_orderitem ON PurchaseOrderReceiptItems(PurchaseOrderItemId);

-- Supplier Performance Reviews Indexes
CREATE INDEX IF NOT EXISTS idx_supplierperformancereviews_supplier ON SupplierPerformanceReviews(SupplierId);
CREATE INDEX IF NOT EXISTS idx_supplierperformancereviews_tenant ON SupplierPerformanceReviews(TenantId);
CREATE INDEX IF NOT EXISTS idx_supplierperformancereviews_period ON SupplierPerformanceReviews(ReviewPeriod);
CREATE INDEX IF NOT EXISTS idx_supplierperformancereviews_date ON SupplierPerformanceReviews(ReviewDate);

-- Supplier Incidents Indexes
CREATE INDEX IF NOT EXISTS idx_supplierincidents_supplier ON SupplierIncidents(SupplierId);
CREATE INDEX IF NOT EXISTS idx_supplierincidents_tenant ON SupplierIncidents(TenantId);
CREATE INDEX IF NOT EXISTS idx_supplierincidents_type ON SupplierIncidents(IncidentType);
CREATE INDEX IF NOT EXISTS idx_supplierincidents_severity ON SupplierIncidents(Severity);
CREATE INDEX IF NOT EXISTS idx_supplierincidents_status ON SupplierIncidents(ResolutionStatus);

-- Supplier Contracts Indexes
CREATE INDEX IF NOT EXISTS idx_suppliercontracts_supplier ON SupplierContracts(SupplierId);
CREATE INDEX IF NOT EXISTS idx_suppliercontracts_tenant ON SupplierContracts(TenantId);
CREATE INDEX IF NOT EXISTS idx_suppliercontracts_number ON SupplierContracts(ContractNumber);
CREATE INDEX IF NOT EXISTS idx_suppliercontracts_status ON SupplierContracts(ContractStatus);
CREATE INDEX IF NOT EXISTS idx_suppliercontracts_dates ON SupplierContracts(StartDate, EndDate);

-- Supplier Price Lists Indexes
CREATE INDEX IF NOT EXISTS idx_supplierpricelists_supplier ON SupplierPriceLists(SupplierId);
CREATE INDEX IF NOT EXISTS idx_supplierpricelists_tenant ON SupplierPriceLists(TenantId);
CREATE INDEX IF NOT EXISTS idx_supplierpricelists_type ON SupplierPriceLists(PriceListType);
CREATE INDEX IF NOT EXISTS idx_supplierpricelists_status ON SupplierPriceLists(Status);
CREATE INDEX IF NOT EXISTS idx_supplierpricelists_dates ON SupplierPriceLists(EffectiveDate, ExpiryDate);

-- Supplier Price List Items Indexes
CREATE INDEX IF NOT EXISTS idx_supplierpricelistitems_pricelist ON SupplierPriceListItems(PriceListId);
CREATE INDEX IF NOT EXISTS idx_supplierpricelistitems_product ON SupplierPriceListItems(ProductId);
CREATE INDEX IF NOT EXISTS idx_supplierpricelistitems_inventory ON SupplierPriceListItems(InventoryItemId);
CREATE INDEX IF NOT EXISTS idx_supplierpricelistitems_available ON SupplierPriceListItems(IsAvailable);

-- ========================================
-- Triggers for Supplier Management Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to supplier tables
CREATE TRIGGER update_suppliers_updated_at BEFORE UPDATE ON Suppliers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_suppliercontacts_updated_at BEFORE UPDATE ON SupplierContacts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_supplierproducts_updated_at BEFORE UPDATE ON SupplierProducts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_purchaseorders_updated_at BEFORE UPDATE ON PurchaseOrders 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_purchaseorderitems_updated_at BEFORE UPDATE ON PurchaseOrderItems 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_purchaseorderreceipts_updated_at BEFORE UPDATE ON PurchaseOrderReceipts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_supplierperformancereviews_updated_at BEFORE UPDATE ON SupplierPerformanceReviews 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_supplierincidents_updated_at BEFORE UPDATE ON SupplierIncidents 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_suppliercontracts_updated_at BEFORE UPDATE ON SupplierContracts 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_supplierpricelists_updated_at BEFORE UPDATE ON SupplierPriceLists 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_supplierpricelistitems_updated_at BEFORE UPDATE ON SupplierPriceListItems 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to generate purchase order numbers
CREATE OR REPLACE FUNCTION generate_purchase_order_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.PurchaseOrderNumber := 'PO-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP)::text, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_purchase_order_number_trigger
    BEFORE INSERT ON PurchaseOrders
    FOR EACH ROW EXECUTE FUNCTION generate_purchase_order_number();

-- Function to generate receipt numbers
CREATE OR REPLACE FUNCTION generate_receipt_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.ReceiptNumber := 'GRN-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP)::text, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_receipt_number_trigger
    BEFORE INSERT ON PurchaseOrderReceipts
    FOR EACH ROW EXECUTE FUNCTION generate_receipt_number_trigger();

-- ========================================
-- Views for Supplier Management
-- ========================================

-- View for Supplier Summary
CREATE OR REPLACE VIEW SupplierSummary AS
SELECT 
    s.Id,
    s.SupplierCode,
    s.BusinessName,
    s.BusinessType,
    s.SupplierCategory,
    s.SupplierStatus,
    s.ContactPerson,
    s.PrimaryPhoneNumber,
    s.Email,
    s.OverallRating,
    s.OnTimeDeliveryRate,
    s.IsPreferred,
    s.TenantId,
    t.BusinessName as TenantName,
    s.CreatedAt,
    CASE 
        WHEN s.IsBlacklisted THEN 'Blacklisted'
        WHEN NOT s.IsActive THEN 'Inactive'
        WHEN s.SupplierStatus = 'Active' THEN 'Active'
        ELSE s.SupplierStatus
    END as CurrentStatus
FROM Suppliers s
LEFT JOIN Tenants t ON s.TenantId = t.Id
WHERE s.IsActive = true OR s.IsBlacklisted = true;

-- View for Purchase Order Status
CREATE OR REPLACE VIEW PurchaseOrderStatus AS
SELECT 
    po.Id,
    po.PurchaseOrderNumber,
    s.BusinessName as SupplierName,
    po.OrderDate,
    po.ExpectedDeliveryDate,
    po.OrderStatus,
    po.TotalAmount,
    po.PaymentStatus,
    po.CreatedBy,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    CASE 
        WHEN po.OrderStatus = 'Completed' THEN 'Completed'
        WHEN po.ExpectedDeliveryDate < CURRENT_DATE AND po.OrderStatus NOT IN ('Completed', 'Cancelled') THEN 'Overdue'
        WHEN po.OrderStatus = 'Confirmed' THEN 'Confirmed'
        ELSE po.OrderStatus
    END as StatusIndicator
FROM PurchaseOrders po
JOIN Suppliers s ON po.SupplierId = s.Id
LEFT JOIN UserAccounts u ON po.CreatedBy = u.Id;

-- View for Supplier Performance Dashboard
CREATE OR REPLACE VIEW SupplierPerformanceDashboard AS
SELECT 
    s.Id,
    s.BusinessName,
    s.SupplierCategory,
    s.OverallRating,
    s.OnTimeDeliveryRate,
    COUNT(DISTINCT po.Id) as TotalPurchaseOrders,
    COUNT(DISTINCT CASE WHEN po.OrderStatus = 'Completed' THEN po.Id END) as CompletedOrders,
    COUNT(DISTINCT spi.Id) as TotalIncidents,
    COUNT(DISTINCT CASE WHEN spi.ResolutionStatus = 'Open' THEN spi.Id END) as OpenIncidents,
    spr.OverallScore as LatestReviewScore,
    spr.PerformanceGrade as LatestGrade,
    spr.ReviewDate as LastReviewDate
FROM Suppliers s
LEFT JOIN PurchaseOrders po ON s.Id = po.SupplierId AND po.OrderDate >= CURRENT_DATE - INTERVAL '12 months'
LEFT JOIN SupplierIncidents spi ON s.Id = spi.SupplierId AND spi.IncidentDate >= CURRENT_DATE - INTERVAL '12 months'
LEFT JOIN SupplierPerformanceReviews spr ON s.Id = spr.SupplierId AND spr.ReviewDate = (
    SELECT MAX(ReviewDate) 
    FROM SupplierPerformanceReviews 
    WHERE SupplierId = s.Id
)
GROUP BY s.Id, s.BusinessName, s.SupplierCategory, s.OverallRating, s.OnTimeDeliveryRate, 
         spr.OverallScore, spr.PerformanceGrade, spr.ReviewDate;
