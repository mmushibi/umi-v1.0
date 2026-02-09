-- Cashier Portal Tables
-- This file contains SQL for tables primarily used by the Cashier portal

-- ========================================
-- Sales Management Tables
-- ========================================

-- Products Table (for point of sale)
CREATE TABLE IF NOT EXISTS Products (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Category VARCHAR(100),
    Price DECIMAL(10,2) NOT NULL CHECK (Price >= 0),
    Stock INTEGER NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    Barcode VARCHAR(50) UNIQUE,
    Description VARCHAR(500),
    MinStock INTEGER DEFAULT 5,
    IsActive BOOLEAN DEFAULT true,
    RequiresPrescription BOOLEAN DEFAULT false,
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
    CustomerType VARCHAR(20) DEFAULT 'Regular' CHECK (CustomerType IN ('Regular', 'VIP', 'Corporate')),
    DiscountRate DECIMAL(5,2) DEFAULT 0.00 CHECK (DiscountRate >= 0 AND DiscountRate <= 100),
    CreditLimit DECIMAL(10,2) DEFAULT 0.00 CHECK (CreditLimit >= 0),
    CurrentBalance DECIMAL(10,2) DEFAULT 0.00,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Sales Table
CREATE TABLE IF NOT EXISTS Sales (
    Id SERIAL PRIMARY KEY,
    SaleNumber VARCHAR(50) UNIQUE NOT NULL,
    CustomerId INTEGER REFERENCES Customers(Id) ON DELETE RESTRICT,
    CashierId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    Subtotal DECIMAL(10,2) NOT NULL CHECK (Subtotal >= 0),
    Tax DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (Tax >= 0),
    Discount DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (Discount >= 0),
    Total DECIMAL(10,2) NOT NULL CHECK (Total >= 0),
    PaymentMethod VARCHAR(20) NOT NULL CHECK (PaymentMethod IN ('Cash', 'Card', 'Mobile Money', 'Insurance', 'Credit', 'Mixed')),
    CashReceived DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    Change DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    Status VARCHAR(20) DEFAULT 'Completed' CHECK (Status IN ('Pending', 'Completed', 'Cancelled', 'Refunded')),
    SaleType VARCHAR(20) DEFAULT 'Retail' CHECK (SaleType IN ('Retail', 'Wholesale', 'Prescription')),
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Sale Items Table
CREATE TABLE IF NOT EXISTS SaleItems (
    Id SERIAL PRIMARY KEY,
    SaleId INTEGER NOT NULL REFERENCES Sales(Id) ON DELETE CASCADE,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE RESTRICT,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(10,2) NOT NULL CHECK (UnitPrice >= 0),
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (DiscountAmount >= 0),
    TotalPrice DECIMAL(10,2) NOT NULL CHECK (TotalPrice >= 0),
    Prescribed BOOLEAN DEFAULT false,
    PrescriptionId INTEGER REFERENCES Prescriptions(Id) ON DELETE SET NULL
);

-- ========================================
-- Payment Processing Tables
-- ========================================

-- Payment Transactions Table (detailed payment tracking)
CREATE TABLE IF NOT EXISTS PaymentTransactions (
    Id SERIAL PRIMARY KEY,
    SaleId INTEGER NOT NULL REFERENCES Sales(Id) ON DELETE CASCADE,
    PaymentMethod VARCHAR(20) NOT NULL,
    Amount DECIMAL(10,2) NOT NULL CHECK (Amount >= 0),
    TransactionReference VARCHAR(100),
    CardLast4 VARCHAR(4),
    MobileMoneyProvider VARCHAR(50),
    MobileMoneyNumber VARCHAR(20),
    ApprovalCode VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Completed' CHECK (Status IN ('Pending', 'Completed', 'Failed', 'Refunded')),
    ProcessedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Refunds Table
CREATE TABLE IF NOT EXISTS Refunds (
    Id SERIAL PRIMARY KEY,
    SaleId INTEGER NOT NULL REFERENCES Sales(Id) ON DELETE CASCADE,
    RefundNumber VARCHAR(50) UNIQUE NOT NULL,
    RefundAmount DECIMAL(10,2) NOT NULL CHECK (RefundAmount > 0),
    RefundReason VARCHAR(500) NOT NULL,
    RefundMethod VARCHAR(20) NOT NULL CHECK (RefundMethod IN ('Cash', 'Card', 'Mobile Money', 'Store Credit')),
    ProcessedBy INTEGER REFERENCES Users(Id),
    Status VARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Processed', 'Rejected')),
    ApprovedBy INTEGER REFERENCES Users(Id),
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    ProcessedAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Refund Items Table
CREATE TABLE IF NOT EXISTS RefundItems (
    Id SERIAL PRIMARY KEY,
    RefundId INTEGER NOT NULL REFERENCES Refunds(Id) ON DELETE CASCADE,
    SaleItemId INTEGER NOT NULL REFERENCES SaleItems(Id) ON DELETE RESTRICT,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    RefundAmount DECIMAL(10,2) NOT NULL CHECK (RefundAmount >= 0),
    Reason VARCHAR(200)
);

-- ========================================
-- Cash Management Tables
-- ========================================

-- Cash Drawer Table (for shift management)
CREATE TABLE IF NOT EXISTS CashDrawers (
    Id SERIAL PRIMARY KEY,
    CashierId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    ShiftStart TIMESTAMP WITH TIME ZONE NOT NULL,
    ShiftEnd TIMESTAMP WITH TIME ZONE,
    OpeningBalance DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (OpeningBalance >= 0),
    ClosingBalance DECIMAL(10,2) DEFAULT 0.00,
    ExpectedCash DECIMAL(10,2) DEFAULT 0.00,
    CashSales DECIMAL(10,2) DEFAULT 0.00,
    CardSales DECIMAL(10,2) DEFAULT 0.00,
    MobileMoneySales DECIMAL(10,2) DEFAULT 0.00,
    OtherSales DECIMAL(10,2) DEFAULT 0.00,
    TotalSales DECIMAL(10,2) DEFAULT 0.00,
    OverShortAmount DECIMAL(10,2) DEFAULT 0.00,
    Status VARCHAR(20) DEFAULT 'Open' CHECK (Status IN ('Open', 'Closed', 'Reconciled')),
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Cash Transactions Table (individual cash movements)
CREATE TABLE IF NOT EXISTS CashTransactions (
    Id SERIAL PRIMARY KEY,
    CashDrawerId INTEGER NOT NULL REFERENCES CashDrawers(Id) ON DELETE CASCADE,
    TransactionType VARCHAR(20) NOT NULL CHECK (TransactionType IN ('Sale', 'Refund', 'Paid In', 'Paid Out', 'Opening', 'Closing')),
    Amount DECIMAL(10,2) NOT NULL,
    ReferenceType VARCHAR(20), -- 'Sale', 'Refund', 'Adjustment'
    ReferenceId INTEGER,
    Description VARCHAR(200),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Customer Loyalty and Promotions Tables
-- ========================================

-- Loyalty Programs Table
CREATE TABLE IF NOT EXISTS LoyaltyPrograms (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    PointsPerDollar DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    MinimumPurchase DECIMAL(10,2) DEFAULT 0.00,
    IsActive BOOLEAN DEFAULT true,
    StartDate DATE,
    EndDate DATE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Customer Loyalty Points Table
CREATE TABLE IF NOT EXISTS CustomerLoyaltyPoints (
    Id SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(Id) ON DELETE CASCADE,
    LoyaltyProgramId INTEGER REFERENCES LoyaltyPrograms(Id) ON DELETE SET NULL,
    Points INTEGER NOT NULL DEFAULT 0,
    TotalPointsEarned INTEGER NOT NULL DEFAULT 0,
    TotalPointsRedeemed INTEGER NOT NULL DEFAULT 0,
    LastActivityDate DATE DEFAULT CURRENT_DATE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Loyalty Transactions Table (points earned/redeemed)
CREATE TABLE IF NOT EXISTS LoyaltyTransactions (
    Id SERIAL PRIMARY KEY,
    CustomerLoyaltyPointsId INTEGER NOT NULL REFERENCES CustomerLoyaltyPoints(Id) ON DELETE CASCADE,
    TransactionType VARCHAR(20) NOT NULL CHECK (TransactionType IN ('Earned', 'Redeemed', 'Expired', 'Adjusted')),
    Points INTEGER NOT NULL,
    ReferenceType VARCHAR(20), -- 'Sale', 'Refund', 'Adjustment'
    ReferenceId INTEGER,
    Description VARCHAR(200),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Promotions Table
CREATE TABLE IF NOT EXISTS Promotions (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    PromotionType VARCHAR(20) NOT NULL CHECK (PromotionType IN ('Discount', 'BuyOneGetOne', 'Bundle', 'LoyaltyMultiplier')),
    DiscountType VARCHAR(20) CHECK (DiscountType IN ('Percentage', 'Fixed', 'BuyXGetY')),
    DiscountValue DECIMAL(10,2),
    MinimumPurchase DECIMAL(10,2) DEFAULT 0.00,
    MaximumDiscount DECIMAL(10,2),
    ApplicableProducts TEXT, -- JSON array of product IDs
    ApplicableCategories TEXT, -- JSON array of categories
    IsActive BOOLEAN DEFAULT true,
    StartDate TIMESTAMP WITH TIME ZONE,
    EndDate TIMESTAMP WITH TIME ZONE,
    UsageLimit INTEGER,
    UsageCount INTEGER DEFAULT 0,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Shift Management Tables
-- ========================================

-- Shifts Table
CREATE TABLE IF NOT EXISTS Shifts (
    Id SERIAL PRIMARY KEY,
    CashierId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    ShiftType VARCHAR(20) NOT NULL CHECK (ShiftType IN ('Morning', 'Afternoon', 'Evening', 'Night')),
    ScheduledStart TIMESTAMP WITH TIME ZONE NOT NULL,
    ScheduledEnd TIMESTAMP WITH TIME ZONE NOT NULL,
    ActualStart TIMESTAMP WITH TIME ZONE,
    ActualEnd TIMESTAMP WITH TIME ZONE,
    Status VARCHAR(20) DEFAULT 'Scheduled' CHECK (Status IN ('Scheduled', 'InProgress', 'Completed', 'Missed')),
    BreakDuration INTEGER DEFAULT 0, -- minutes
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Shift Handovers Table (for shift transitions)
CREATE TABLE IF NOT EXISTS ShiftHandovers (
    Id SERIAL PRIMARY KEY,
    FromShiftId INTEGER REFERENCES Shifts(Id) ON DELETE SET NULL,
    ToShiftId INTEGER REFERENCES Shifts(Id) ON DELETE SET NULL,
    FromCashierId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    ToCashierId INTEGER REFERENCES Users(Id) ON DELETE SET NULL,
    HandoverTime TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CashBalance DECIMAL(10,2) NOT NULL,
    CardSalesTotal DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    MobileMoneyTotal DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    OtherPaymentsTotal DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    TotalSales DECIMAL(10,2) NOT NULL,
    DiscrepancyAmount DECIMAL(10,2) DEFAULT 0.00,
    Notes VARCHAR(500),
    Status VARCHAR(20) DEFAULT 'Completed' CHECK (Status IN ('Pending', 'Completed', 'Disputed'))
);

-- ========================================
-- Indexes for Cashier Portal Tables
-- ========================================

-- Products Indexes
CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);
CREATE INDEX IF NOT EXISTS idx_products_category ON Products(Category);
CREATE INDEX IF NOT EXISTS idx_products_active ON Products(IsActive);
CREATE INDEX IF NOT EXISTS idx_products_prescription_required ON Products(RequiresPrescription);

-- Customers Indexes
CREATE INDEX IF NOT EXISTS idx_customers_name ON Customers(Name);
CREATE INDEX IF NOT EXISTS idx_customers_email ON Customers(Email);
CREATE INDEX IF NOT EXISTS idx_customers_phone ON Customers(Phone);
CREATE INDEX IF NOT EXISTS idx_customers_type ON Customers(CustomerType);
CREATE INDEX IF NOT EXISTS idx_customers_active ON Customers(IsActive);

-- Sales Indexes
CREATE INDEX IF NOT EXISTS idx_sales_number ON Sales(SaleNumber);
CREATE INDEX IF NOT EXISTS idx_sales_customer ON Sales(CustomerId);
CREATE INDEX IF NOT EXISTS idx_sales_cashier ON Sales(CashierId);
CREATE INDEX IF NOT EXISTS idx_sales_date ON Sales(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_sales_status ON Sales(Status);
CREATE INDEX IF NOT EXISTS idx_sales_payment_method ON Sales(PaymentMethod);

-- Sale Items Indexes
CREATE INDEX IF NOT EXISTS idx_saleitems_sale ON SaleItems(SaleId);
CREATE INDEX IF NOT EXISTS idx_saleitems_product ON SaleItems(ProductId);
CREATE INDEX IF NOT EXISTS idx_saleitems_prescription ON SaleItems(PrescriptionId);

-- Payment Transactions Indexes
CREATE INDEX IF NOT EXISTS idx_paymenttransactions_sale ON PaymentTransactions(SaleId);
CREATE INDEX IF NOT EXISTS idx_paymenttransactions_method ON PaymentTransactions(PaymentMethod);
CREATE INDEX IF NOT EXISTS idx_paymenttransactions_status ON PaymentTransactions(Status);
CREATE INDEX IF NOT EXISTS idx_paymenttransactions_date ON PaymentTransactions(ProcessedAt);

-- Refunds Indexes
CREATE INDEX IF NOT EXISTS idx_refunds_sale ON Refunds(SaleId);
CREATE INDEX IF NOT EXISTS idx_refunds_number ON Refunds(RefundNumber);
CREATE INDEX IF NOT EXISTS idx_refunds_status ON Refunds(Status);
CREATE INDEX IF NOT EXISTS idx_refunds_date ON Refunds(CreatedAt);

-- Cash Drawers Indexes
CREATE INDEX IF NOT EXISTS idx_cashdrawers_cashier ON CashDrawers(CashierId);
CREATE INDEX IF NOT EXISTS idx_cashdrawers_shift_start ON CashDrawers(ShiftStart);
CREATE INDEX IF NOT EXISTS idx_cashdrawers_status ON CashDrawers(Status);

-- Cash Transactions Indexes
CREATE INDEX IF NOT EXISTS idx_cashtransactions_drawer ON CashTransactions(CashDrawerId);
CREATE INDEX IF NOT EXISTS idx_cashtransactions_type ON CashTransactions(TransactionType);
CREATE INDEX IF NOT EXISTS idx_cashtransactions_date ON CashTransactions(CreatedAt);

-- Loyalty Programs Indexes
CREATE INDEX IF NOT EXISTS idx_loyaltyprograms_active ON LoyaltyPrograms(IsActive);
CREATE INDEX IF NOT EXISTS idx_loyaltyprograms_dates ON LoyaltyPrograms(StartDate, EndDate);

-- Customer Loyalty Points Indexes
CREATE INDEX IF NOT EXISTS idx_customerloyaltypoints_customer ON CustomerLoyaltyPoints(CustomerId);
CREATE INDEX IF NOT EXISTS idx_customerloyaltypoints_program ON CustomerLoyaltyPoints(LoyaltyProgramId);

-- Loyalty Transactions Indexes
CREATE INDEX IF NOT EXISTS idx_loyaltytransactions_customerpoints ON LoyaltyTransactions(CustomerLoyaltyPointsId);
CREATE INDEX IF NOT EXISTS idx_loyaltytransactions_type ON LoyaltyTransactions(TransactionType);

-- Promotions Indexes
CREATE INDEX IF NOT EXISTS idx_promotions_active ON Promotions(IsActive);
CREATE INDEX IF NOT EXISTS idx_promotions_dates ON Promotions(StartDate, EndDate);
CREATE INDEX IF NOT EXISTS idx_promotions_type ON Promotions(PromotionType);

-- Shifts Indexes
CREATE INDEX IF NOT EXISTS idx_shifts_cashier ON Shifts(CashierId);
CREATE INDEX IF NOT EXISTS idx_shifts_scheduled ON Shifts(ScheduledStart);
CREATE INDEX IF NOT EXISTS idx_shifts_status ON Shifts(Status);

-- Shift Handovers Indexes
CREATE INDEX IF NOT EXISTS idx_shifthandovers_from_shift ON ShiftHandovers(FromShiftId);
CREATE INDEX IF NOT EXISTS idx_shifthandovers_to_shift ON ShiftHandovers(ToShiftId);
CREATE INDEX IF NOT EXISTS idx_shifthandovers_from_cashier ON ShiftHandovers(FromCashierId);
CREATE INDEX IF NOT EXISTS idx_shifthandovers_to_cashier ON ShiftHandovers(ToCashierId);

-- ========================================
-- Triggers for Cashier Portal Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to Cashier tables
CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON Products 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customers_updated_at BEFORE UPDATE ON Customers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_loyaltyprograms_updated_at BEFORE UPDATE ON LoyaltyPrograms 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customerloyaltypoints_updated_at BEFORE UPDATE ON CustomerLoyaltyPoints 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_promotions_updated_at BEFORE UPDATE ON Promotions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to generate sale numbers
CREATE OR REPLACE FUNCTION generate_sale_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.SaleNumber := 'SALE-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP)::text, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_sale_number_trigger
    BEFORE INSERT ON Sales
    FOR EACH ROW EXECUTE FUNCTION generate_sale_number();

-- Function to generate refund numbers
CREATE OR REPLACE FUNCTION generate_refund_number()
RETURNS TRIGGER AS $$
BEGIN
    NEW.RefundNumber := 'REF-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(EXTRACT(MICROSECONDS FROM CURRENT_TIMESTAMP)::text, 6, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_refund_number_trigger
    BEFORE INSERT ON Refunds
    FOR EACH ROW EXECUTE FUNCTION generate_refund_number();

-- ========================================
-- Views for Cashier Dashboard
-- ========================================

-- View for Today's Sales Summary
CREATE OR REPLACE VIEW TodaySalesSummary AS
SELECT 
    COUNT(*) as TotalTransactions,
    SUM(Total) as TotalRevenue,
    SUM(CASE WHEN PaymentMethod = 'Cash' THEN Total ELSE 0 END) as CashSales,
    SUM(CASE WHEN PaymentMethod = 'Card' THEN Total ELSE 0 END) as CardSales,
    SUM(CASE WHEN PaymentMethod = 'Mobile Money' THEN Total ELSE 0 END) as MobileMoneySales,
    AVG(Total) as AverageTransaction,
    COUNT(DISTINCT CustomerId) as UniqueCustomers
FROM Sales 
WHERE DATE(CreatedAt) = CURRENT_DATE 
  AND Status = 'Completed';

-- View for Top Selling Products
CREATE OR REPLACE VIEW TopSellingProducts AS
SELECT 
    p.Id,
    p.Name,
    p.Category,
    SUM(si.Quantity) as TotalQuantitySold,
    SUM(si.TotalPrice) as TotalRevenue,
    COUNT(DISTINCT si.SaleId) as NumberOfSales
FROM Products p
JOIN SaleItems si ON p.Id = si.ProductId
JOIN Sales s ON si.SaleId = s.Id
WHERE DATE(s.CreatedAt) = CURRENT_DATE 
  AND s.Status = 'Completed'
GROUP BY p.Id, p.Name, p.Category
ORDER BY TotalQuantitySold DESC
LIMIT 10;

-- View for Cashier Performance
CREATE OR REPLACE VIEW CashierPerformance AS
SELECT 
    u.Id as CashierId,
    u.FirstName || ' ' || u.LastName as CashierName,
    COUNT(s.Id) as TotalTransactions,
    SUM(s.Total) as TotalRevenue,
    AVG(s.Total) as AverageTransaction,
    COUNT(DISTINCT DATE(s.CreatedAt)) as DaysWorked
FROM Users u
LEFT JOIN Sales s ON u.Id = s.CashierId
WHERE u.Role = 'Cashier'
  AND u.IsActive = true
GROUP BY u.Id, u.FirstName, u.LastName
ORDER BY TotalRevenue DESC;

-- View for Current Shift Status
CREATE OR REPLACE VIEW CurrentShiftStatus AS
SELECT 
    cd.Id,
    u.FirstName || ' ' || u.LastName as CashierName,
    cd.ShiftStart,
    cd.OpeningBalance,
    cd.CashSales,
    cd.CardSales,
    cd.MobileMoneySales,
    cd.TotalSales,
    cd.Status
FROM CashDrawers cd
JOIN Users u ON cd.CashierId = u.Id
WHERE cd.Status = 'Open'
ORDER BY cd.ShiftStart DESC;
