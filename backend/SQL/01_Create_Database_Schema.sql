-- Umi Health POS Database Schema
-- PostgreSQL Database: umi_db
-- Created for all backend and frontend pages

-- ========================================
-- Core Tables
-- ========================================

-- Products Table (for Sales Operations)
CREATE TABLE IF NOT EXISTS Products (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Category VARCHAR(100),
    Price DECIMAL(10,2) NOT NULL,
    Stock INTEGER NOT NULL DEFAULT 0,
    Barcode VARCHAR(50) UNIQUE,
    Description VARCHAR(500),
    MinStock INTEGER DEFAULT 5,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Customers Table
CREATE TABLE IF NOT EXISTS Customers (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE,
    Phone VARCHAR(20),
    Address VARCHAR(200),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Sales Table
CREATE TABLE IF NOT EXISTS Sales (
    Id SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(Id) ON DELETE RESTRICT,
    Subtotal DECIMAL(10,2) NOT NULL,
    Tax DECIMAL(10,2) NOT NULL DEFAULT 0,
    Total DECIMAL(10,2) NOT NULL,
    PaymentMethod VARCHAR(20) NOT NULL,
    CashReceived DECIMAL(10,2) NOT NULL,
    Change DECIMAL(10,2) NOT NULL,
    Status VARCHAR(20) DEFAULT 'Completed',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Sale Items Table
CREATE TABLE IF NOT EXISTS SaleItems (
    Id SERIAL PRIMARY KEY,
    SaleId INTEGER NOT NULL REFERENCES Sales(Id) ON DELETE CASCADE,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE RESTRICT,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Quantity INTEGER NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL
);

-- Stock Transactions Table
CREATE TABLE IF NOT EXISTS StockTransactions (
    Id SERIAL PRIMARY KEY,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    TransactionType VARCHAR(50) NOT NULL,
    QuantityChange INTEGER NOT NULL,
    PreviousStock INTEGER NOT NULL,
    NewStock INTEGER NOT NULL,
    Reason VARCHAR(200),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Inventory Management Tables
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
    Quantity INTEGER NOT NULL DEFAULT 0,
    UnitPrice DECIMAL(10,2) NOT NULL,
    SellingPrice DECIMAL(10,2) NOT NULL,
    ReorderLevel INTEGER DEFAULT 10,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Pharmacy/Patient Management Tables
-- ========================================

-- Patients Table
CREATE TABLE IF NOT EXISTS Patients (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    IdNumber VARCHAR(20) UNIQUE,
    PhoneNumber VARCHAR(100),
    Email VARCHAR(100),
    DateOfBirth DATE,
    Gender VARCHAR(10),
    Address VARCHAR(200),
    Allergies VARCHAR(100),
    MedicalHistory VARCHAR(500),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Prescriptions Table
CREATE TABLE IF NOT EXISTS Prescriptions (
    Id SERIAL PRIMARY KEY,
    RxNumber VARCHAR(50) NOT NULL UNIQUE,
    PatientId INTEGER NOT NULL REFERENCES Patients(Id) ON DELETE RESTRICT,
    PatientName VARCHAR(200) NOT NULL,
    PatientIdNumber VARCHAR(20),
    DoctorName VARCHAR(200) NOT NULL,
    DoctorRegistrationNumber VARCHAR(100),
    Medication VARCHAR(300) NOT NULL,
    Dosage VARCHAR(200) NOT NULL,
    Instructions VARCHAR(200) NOT NULL,
    TotalCost DECIMAL(10,2) NOT NULL,
    Status VARCHAR(20) DEFAULT 'pending',
    PrescriptionDate DATE NOT NULL DEFAULT CURRENT_DATE,
    ExpiryDate DATE,
    FilledDate DATE,
    Notes VARCHAR(500),
    IsUrgent BOOLEAN DEFAULT false,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Prescription Items Table
CREATE TABLE IF NOT EXISTS PrescriptionItems (
    Id SERIAL PRIMARY KEY,
    PrescriptionId INTEGER NOT NULL REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    InventoryItemId INTEGER NOT NULL REFERENCES InventoryItems(Id) ON DELETE RESTRICT,
    MedicationName VARCHAR(200) NOT NULL,
    Dosage VARCHAR(100) NOT NULL,
    Quantity INTEGER NOT NULL,
    Instructions VARCHAR(200) NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- User Management Tables (for Multi-Portal System)
-- ========================================

-- Users Table
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Role VARCHAR(20) NOT NULL, -- 'TenantAdmin', 'Pharmacist', 'Cashier', 'SuperAdmin'
    TenantId INTEGER,
    IsActive BOOLEAN DEFAULT true,
    LastLogin TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tenants Table (for multi-tenant support)
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

-- ========================================
-- Dashboard & Analytics Tables
-- ========================================

-- Dashboard Settings Table
CREATE TABLE IF NOT EXISTS DashboardSettings (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    WidgetLayout TEXT,
    Theme VARCHAR(20) DEFAULT 'light',
    Language VARCHAR(10) DEFAULT 'en',
    TimeZone VARCHAR(50) DEFAULT 'Africa/Lusaka',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Audit Log Table
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER REFERENCES Users(Id),
    Action VARCHAR(100) NOT NULL,
    TableName VARCHAR(50),
    RecordId INTEGER,
    OldValues TEXT,
    NewValues TEXT,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- System Configuration Tables
-- ========================================

-- System Settings Table
CREATE TABLE IF NOT EXISTS SystemSettings (
    Id SERIAL PRIMARY KEY,
    SettingKey VARCHAR(100) UNIQUE NOT NULL,
    SettingValue TEXT,
    Description VARCHAR(500),
    Category VARCHAR(50),
    IsEditable BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Notifications Table
CREATE TABLE IF NOT EXISTS Notifications (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER REFERENCES Users(Id) ON DELETE CASCADE,
    TenantId INTEGER REFERENCES Tenants(Id) ON DELETE CASCADE,
    Title VARCHAR(200) NOT NULL,
    Message TEXT NOT NULL,
    Type VARCHAR(20) DEFAULT 'info', -- 'info', 'warning', 'error', 'success'
    IsRead BOOLEAN DEFAULT false,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ReadAt TIMESTAMP WITH TIME ZONE
);

-- ========================================
-- Indexes for Performance Optimization
-- ========================================

-- Products Indexes
CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
CREATE INDEX IF NOT EXISTS idx_products_category ON Products(Category);
CREATE INDEX IF NOT EXISTS idx_products_active ON Products(IsActive);
CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);

-- Customers Indexes
CREATE INDEX IF NOT EXISTS idx_customers_email ON Customers(Email);
CREATE INDEX IF NOT EXISTS idx_customers_name ON Customers(Name);
CREATE INDEX IF NOT EXISTS idx_customers_active ON Customers(IsActive);

-- Sales Indexes
CREATE INDEX IF NOT EXISTS idx_sales_customer ON Sales(CustomerId);
CREATE INDEX IF NOT EXISTS idx_sales_date ON Sales(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_sales_status ON Sales(Status);

-- Sale Items Indexes
CREATE INDEX IF NOT EXISTS idx_saleitems_sale ON SaleItems(SaleId);
CREATE INDEX IF NOT EXISTS idx_saleitems_product ON SaleItems(ProductId);

-- Stock Transactions Indexes
CREATE INDEX IF NOT EXISTS idx_stocktransactions_product ON StockTransactions(ProductId);
CREATE INDEX IF NOT EXISTS idx_stocktransactions_date ON StockTransactions(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_stocktransactions_type ON StockTransactions(TransactionType);

-- Inventory Items Indexes
CREATE INDEX IF NOT EXISTS idx_inventoryitems_batch ON InventoryItems(BatchNumber);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_zambia_reg ON InventoryItems(ZambiaRegNumber);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_name ON InventoryItems(InventoryItemName);
CREATE INDEX IF NOT EXISTS idx_inventoryitems_active ON InventoryItems(IsActive);

-- Patients Indexes
CREATE INDEX IF NOT EXISTS idx_patients_id_number ON Patients(IdNumber);
CREATE INDEX IF NOT EXISTS idx_patients_name ON Patients(Name);
CREATE INDEX IF NOT EXISTS idx_patients_active ON Patients(IsActive);

-- Prescriptions Indexes
CREATE INDEX IF NOT EXISTS idx_prescriptions_rx ON Prescriptions(RxNumber);
CREATE INDEX IF NOT EXISTS idx_prescriptions_patient ON Prescriptions(PatientId);
CREATE INDEX IF NOT EXISTS idx_prescriptions_status ON Prescriptions(Status);
CREATE INDEX IF NOT EXISTS idx_prescriptions_date ON Prescriptions(PrescriptionDate);

-- Prescription Items Indexes
CREATE INDEX IF NOT EXISTS idx_prescriptionitems_prescription ON PrescriptionItems(PrescriptionId);
CREATE INDEX IF NOT EXISTS idx_prescriptionitems_inventory ON PrescriptionItems(InventoryItemId);

-- Users Indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_role ON Users(Role);
CREATE INDEX IF NOT EXISTS idx_users_tenant ON Users(TenantId);
CREATE INDEX IF NOT EXISTS idx_users_active ON Users(IsActive);

-- Notifications Indexes
CREATE INDEX IF NOT EXISTS idx_notifications_user ON Notifications(UserId);
CREATE INDEX IF NOT EXISTS idx_notifications_tenant ON Notifications(TenantId);
CREATE INDEX IF NOT EXISTS idx_notifications_read ON Notifications(IsRead);
CREATE INDEX IF NOT EXISTS idx_notifications_created ON Notifications(CreatedAt);

-- Audit Logs Indexes
CREATE INDEX IF NOT EXISTS idx_auditlogs_user ON AuditLogs(UserId);
CREATE INDEX IF NOT EXISTS idx_auditlogs_table ON AuditLogs(TableName);
CREATE INDEX IF NOT EXISTS idx_auditlogs_date ON AuditLogs(CreatedAt);

-- ========================================
-- Triggers and Functions
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to relevant tables
CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON Products 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customers_updated_at BEFORE UPDATE ON Customers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_inventoryitems_updated_at BEFORE UPDATE ON InventoryItems 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_patients_updated_at BEFORE UPDATE ON Patients 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_prescriptions_updated_at BEFORE UPDATE ON Prescriptions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON Users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tenants_updated_at BEFORE UPDATE ON Tenants 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_dashboardsettings_updated_at BEFORE UPDATE ON DashboardSettings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_systemsettings_updated_at BEFORE UPDATE ON SystemSettings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ========================================
-- Constraints and Additional Rules
-- ========================================

-- Check constraints for data integrity
ALTER TABLE Products ADD CONSTRAINT chk_products_stock_positive 
    CHECK (Stock >= 0);

ALTER TABLE Products ADD CONSTRAINT chk_products_price_positive 
    CHECK (Price >= 0);

ALTER TABLE InventoryItems ADD CONSTRAINT chk_inventoryitems_quantity_positive 
    CHECK (Quantity >= 0);

ALTER TABLE InventoryItems ADD CONSTRAINT chk_inventoryitems_unit_price_positive 
    CHECK (UnitPrice >= 0);

ALTER TABLE InventoryItems ADD CONSTRAINT chk_inventoryitems_selling_price_positive 
    CHECK (SellingPrice >= 0);

ALTER TABLE Sales ADD CONSTRAINT chk_sales_total_positive 
    CHECK (Total >= 0);

ALTER TABLE SaleItems ADD CONSTRAINT chk_saleitems_quantity_positive 
    CHECK (Quantity >= 0);

ALTER TABLE SaleItems ADD CONSTRAINT chk_saleitems_unit_price_positive 
    CHECK (UnitPrice >= 0);

ALTER TABLE PrescriptionItems ADD CONSTRAINT chk_prescriptionitems_quantity_positive 
    CHECK (Quantity >= 0);

ALTER TABLE PrescriptionItems ADD CONSTRAINT chk_prescriptionitems_unit_price_positive 
    CHECK (UnitPrice >= 0);

-- Users role constraint
ALTER TABLE Users ADD CONSTRAINT chk_users_role 
    CHECK (Role IN ('TenantAdmin', 'Pharmacist', 'Cashier', 'SuperAdmin'));

-- Prescriptions status constraint
ALTER TABLE Prescriptions ADD CONSTRAINT chk_prescriptions_status 
    CHECK (Status IN ('pending', 'filled', 'expired', 'cancelled'));

-- Notifications type constraint
ALTER TABLE Notifications ADD CONSTRAINT chk_notifications_type 
    CHECK (Type IN ('info', 'warning', 'error', 'success'));

-- Stock transaction type constraint
ALTER TABLE StockTransactions ADD CONSTRAINT chk_stocktransactions_type 
    CHECK (TransactionType IN ('Sale', 'Purchase', 'Adjustment', 'Return'));

-- Payment method constraint
ALTER TABLE Sales ADD CONSTRAINT chk_sales_payment_method 
    CHECK (PaymentMethod IN ('Cash', 'Card', 'Mobile Money', 'Insurance', 'Credit'));

-- ========================================
-- Views for Common Queries
-- ========================================

-- View for Low Stock Items
CREATE OR REPLACE VIEW LowStockItems AS
SELECT 
    Id,
    InventoryItemName,
    GenericName,
    BrandName,
    BatchNumber,
    Quantity,
    ReorderLevel,
    UnitPrice,
    SellingPrice
FROM InventoryItems 
WHERE IsActive = true 
  AND Quantity <= ReorderLevel
ORDER BY Quantity ASC;

-- View for Today's Sales
CREATE OR REPLACE VIEW TodaySales AS
SELECT 
    s.Id,
    s.Total,
    s.PaymentMethod,
    s.Status,
    c.Name as CustomerName,
    s.CreatedAt
FROM Sales s
JOIN Customers c ON s.CustomerId = c.Id
WHERE DATE(s.CreatedAt) = CURRENT_DATE
ORDER BY s.CreatedAt DESC;

-- View for Pending Prescriptions
CREATE OR REPLACE VIEW PendingPrescriptions AS
SELECT 
    p.Id,
    p.RxNumber,
    p.PatientName,
    p.DoctorName,
    p.Medication,
    p.PrescriptionDate,
    p.IsUrgent,
    p.CreatedAt
FROM Prescriptions p
WHERE p.Status = 'pending'
ORDER BY p.IsUrgent DESC, p.CreatedAt ASC;

-- View for User Dashboard Stats
CREATE OR REPLACE VIEW DashboardStats AS
SELECT 
    (SELECT COUNT(*) FROM Sales WHERE DATE(CreatedAt) = CURRENT_DATE) as TodaySales,
    (SELECT COUNT(*) FROM Prescriptions WHERE Status = 'pending') as PendingPrescriptions,
    (SELECT COUNT(*) FROM InventoryItems WHERE IsActive = true AND Quantity <= ReorderLevel) as LowStockItems,
    (SELECT COUNT(*) FROM Patients WHERE IsActive = true) as ActivePatients,
    (SELECT COALESCE(SUM(Total), 0) FROM Sales WHERE DATE(CreatedAt) = CURRENT_DATE) as TodayRevenue;

-- ========================================
-- Initial System Settings
-- ========================================

INSERT INTO SystemSettings (SettingKey, SettingValue, Description, Category) VALUES
('business_name', 'Umi Health Pharmacy', 'Business name for receipts and reports', 'General'),
('business_address', 'Lusaka, Zambia', 'Business address', 'General'),
('business_phone', '+260 123 456 789', 'Business phone number', 'General'),
('business_email', 'info@umihealth.com', 'Business email address', 'General'),
('currency_code', 'ZMW', 'Currency code for transactions', 'Financial'),
('tax_rate', '0.16', 'Default tax rate (16%)', 'Financial'),
('low_stock_notification', 'true', 'Enable low stock notifications', 'Inventory'),
('prescription_expiry_days', '30', 'Default prescription expiry in days', 'Pharmacy'),
('enable_audit_logging', 'true', 'Enable audit logging', 'Security'),
('session_timeout_minutes', '30', 'User session timeout in minutes', 'Security')
ON CONFLICT (SettingKey) DO NOTHING;
