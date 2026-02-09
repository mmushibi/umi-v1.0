-- Sales Operations Portal Tables
-- This file contains SQL for tables primarily used by the Sales Operations portal

-- ========================================
-- Sales Analytics and Reporting Tables
-- ========================================

-- Sales Summary Table (daily aggregated data)
CREATE TABLE IF NOT EXISTS SalesSummary (
    Id SERIAL PRIMARY KEY,
    SummaryDate DATE NOT NULL UNIQUE,
    TotalTransactions INTEGER NOT NULL DEFAULT 0,
    TotalRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalTax DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalDiscount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CashRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CardRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    MobileMoneyRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    OtherRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    AverageTransaction DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    UniqueCustomers INTEGER NOT NULL DEFAULT 0,
    ReturnedTransactions INTEGER NOT NULL DEFAULT 0,
    RefundAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Product Sales Analytics Table (product performance tracking)
CREATE TABLE IF NOT EXISTS ProductSalesAnalytics (
    Id SERIAL PRIMARY KEY,
    ProductId INTEGER NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    AnalyticsDate DATE NOT NULL,
    QuantitySold INTEGER NOT NULL DEFAULT 0,
    Revenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    Cost DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    Profit DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    ProfitMargin DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    NumberOfTransactions INTEGER NOT NULL DEFAULT 0,
    AveragePrice DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(ProductId, AnalyticsDate)
);

-- Customer Analytics Table (customer behavior tracking)
CREATE TABLE IF NOT EXISTS CustomerAnalytics (
    Id SERIAL PRIMARY KEY,
    CustomerId INTEGER REFERENCES Customers(Id) ON DELETE CASCADE,
    AnalyticsDate DATE NOT NULL,
    TotalPurchases INTEGER NOT NULL DEFAULT 0,
    TotalSpent DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    AverageTransaction DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    ItemsPurchased INTEGER NOT NULL DEFAULT 0,
    LastPurchaseDate TIMESTAMP WITH TIME ZONE,
    DaysSinceLastPurchase INTEGER,
    CustomerLifetimeValue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(CustomerId, AnalyticsDate)
);

-- Category Performance Table (category-wise analytics)
CREATE TABLE IF NOT EXISTS CategoryPerformance (
    Id SERIAL PRIMARY KEY,
    Category VARCHAR(100) NOT NULL,
    AnalyticsDate DATE NOT NULL,
    TotalRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    UnitsSold INTEGER NOT NULL DEFAULT 0,
    AveragePrice DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    ProfitMargin DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    MarketShare DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    GrowthRate DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(Category, AnalyticsDate)
);

-- ========================================
-- Subscription and Membership Tables
-- ========================================

-- Subscription Plans Table
CREATE TABLE IF NOT EXISTS SubscriptionPlans (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    PlanType VARCHAR(20) NOT NULL CHECK (PlanType IN ('Basic', 'Premium', 'Enterprise')),
    MonthlyFee DECIMAL(10,2) NOT NULL CHECK (MonthlyFee >= 0),
    AnnualFee DECIMAL(10,2) NOT NULL CHECK (AnnualFee >= 0),
    Features TEXT, -- JSON array of features
    MaxUsers INTEGER,
    MaxProducts INTEGER,
    MaxTransactions INTEGER,
    SupportLevel VARCHAR(20) DEFAULT 'Basic' CHECK (SupportLevel IN ('Basic', 'Priority', 'Dedicated')),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Customer Subscriptions Table
CREATE TABLE IF NOT EXISTS CustomerSubscriptions (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    SubscriptionPlanId INTEGER NOT NULL REFERENCES SubscriptionPlans(Id) ON DELETE RESTRICT,
    SubscriptionNumber VARCHAR(50) UNIQUE NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    BillingCycle VARCHAR(20) NOT NULL CHECK (BillingCycle IN ('Monthly', 'Quarterly', 'Annual')),
    Status VARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Suspended', 'Cancelled', 'Expired')),
    AutoRenew BOOLEAN DEFAULT true,
    NextBillingDate DATE,
    LastPaymentDate DATE,
    CancelledAt DATE,
    CancellationReason VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Subscription Payments Table
CREATE TABLE IF NOT EXISTS SubscriptionPayments (
    Id SERIAL PRIMARY KEY,
    SubscriptionId INTEGER NOT NULL REFERENCES CustomerSubscriptions(Id) ON DELETE CASCADE,
    PaymentNumber VARCHAR(50) UNIQUE NOT NULL,
    Amount DECIMAL(10,2) NOT NULL CHECK (Amount > 0),
    PaymentDate DATE NOT NULL,
    PaymentMethod VARCHAR(20) NOT NULL CHECK (PaymentMethod IN ('Card', 'Bank Transfer', 'Mobile Money', 'Cheque')),
    TransactionReference VARCHAR(100),
    Status VARCHAR(20) DEFAULT 'Completed' CHECK (Status IN ('Pending', 'Completed', 'Failed', 'Refunded')),
    DueDate DATE NOT NULL,
    LateFee DECIMAL(10,2) DEFAULT 0.00,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Sales Targets and Performance Tables
-- ========================================

-- Sales Targets Table
CREATE TABLE IF NOT EXISTS SalesTargets (
    Id SERIAL PRIMARY KEY,
    TenantId INTEGER NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    TargetType VARCHAR(20) NOT NULL CHECK (TargetType IN ('Daily', 'Weekly', 'Monthly', 'Quarterly', 'Annual')),
    TargetPeriod VARCHAR(50) NOT NULL, -- e.g., '2024-01', '2024-W01', '2024-01-15'
    TargetRevenue DECIMAL(15,2) NOT NULL CHECK (TargetRevenue >= 0),
    TargetTransactions INTEGER NOT NULL DEFAULT 0,
    TargetCustomers INTEGER NOT NULL DEFAULT 0,
    AssignedTo INTEGER REFERENCES Users(Id), -- Sales manager or team lead
    Status VARCHAR(20) DEFAULT 'Active' CHECK (Status IN ('Active', 'Achieved', 'Missed', 'Cancelled')),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Sales Performance Table (tracking actual vs targets)
CREATE TABLE IF NOT EXISTS SalesPerformance (
    Id SERIAL PRIMARY KEY,
    TargetId INTEGER NOT NULL REFERENCES SalesTargets(Id) ON DELETE CASCADE,
    ActualRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    ActualTransactions INTEGER NOT NULL DEFAULT 0,
    ActualCustomers INTEGER NOT NULL DEFAULT 0,
    RevenueAchievementPercentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    TransactionAchievementPercentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    CustomerAchievementPercentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    PerformanceDate DATE NOT NULL,
    Notes VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Sales Team Performance Table (team member performance)
CREATE TABLE IF NOT EXISTS SalesTeamPerformance (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    PerformancePeriod VARCHAR(50) NOT NULL, -- e.g., '2024-01', '2024-W01'
    TotalSales DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    TotalTransactions INTEGER NOT NULL DEFAULT 0,
    AverageTransaction DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    NewCustomers INTEGER NOT NULL DEFAULT 0,
    RepeatCustomers INTEGER NOT NULL DEFAULT 0,
    CustomerSatisfactionScore DECIMAL(3,2), -- 1.00 to 5.00
    TargetAchievementPercentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    Ranking INTEGER,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Commission and Incentives Tables
-- ========================================

-- Commission Plans Table
CREATE TABLE IF NOT EXISTS CommissionPlans (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    PlanType VARCHAR(20) NOT NULL CHECK (PlanType IN ('Percentage', 'Tiered', 'Fixed', 'Hybrid')),
    CommissionStructure TEXT, -- JSON structure for commission rules
    MinimumSales DECIMAL(15,2) DEFAULT 0.00,
    MaximumCommission DECIMAL(15,2),
    IsActive BOOLEAN DEFAULT true,
    StartDate DATE,
    EndDate DATE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User Commission Plans Table (assigning commission plans to users)
CREATE TABLE IF NOT EXISTS UserCommissionPlans (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    CommissionPlanId INTEGER NOT NULL REFERENCES CommissionPlans(Id) ON DELETE RESTRICT,
    StartDate DATE NOT NULL,
    EndDate DATE,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Commission Earnings Table
CREATE TABLE IF NOT EXISTS CommissionEarnings (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    SaleId INTEGER REFERENCES Sales(Id) ON DELETE SET NULL,
    CommissionPlanId INTEGER REFERENCES CommissionPlans(Id) ON DELETE SET NULL,
    EarningPeriod VARCHAR(50) NOT NULL, -- e.g., '2024-01'
    CommissionAmount DECIMAL(10,2) NOT NULL CHECK (CommissionAmount >= 0),
    CommissionRate DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    SaleAmount DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    Status VARCHAR(20) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Paid', 'Cancelled')),
    PaidDate DATE,
    PaymentReference VARCHAR(100),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Regional and Territory Management Tables
-- ========================================

-- Sales Territories Table
CREATE TABLE IF NOT EXISTS SalesTerritories (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    TerritoryType VARCHAR(20) NOT NULL CHECK (TerritoryType IN ('Geographic', 'Industry', 'Customer Segment')),
    CoverageArea TEXT, -- JSON or text describing the coverage
    AssignedManagerId INTEGER REFERENCES Users(Id),
    TargetMarket VARCHAR(200),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Territory Assignments Table
CREATE TABLE IF NOT EXISTS TerritoryAssignments (
    Id SERIAL PRIMARY KEY,
    TerritoryId INTEGER NOT NULL REFERENCES SalesTerritories(Id) ON DELETE CASCADE,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    AssignmentDate DATE NOT NULL,
    EndDate DATE,
    AssignmentType VARCHAR(20) DEFAULT 'Primary' CHECK (AssignmentType IN ('Primary', 'Secondary', 'Support')),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Territory Performance Table
CREATE TABLE IF NOT EXISTS TerritoryPerformance (
    Id SERIAL PRIMARY KEY,
    TerritoryId INTEGER NOT NULL REFERENCES SalesTerritories(Id) ON DELETE CASCADE,
    PerformancePeriod VARCHAR(50) NOT NULL,
    TotalRevenue DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    NewCustomers INTEGER NOT NULL DEFAULT 0,
    MarketPenetration DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    GrowthRate DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    CompetitivePosition VARCHAR(20),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- Indexes for Sales Operations Tables
-- ========================================

-- Sales Summary Indexes
CREATE INDEX IF NOT EXISTS idx_salessummary_date ON SalesSummary(SummaryDate);
CREATE INDEX IF NOT EXISTS idx_salessummary_revenue ON SalesSummary(TotalRevenue);

-- Product Sales Analytics Indexes
CREATE INDEX IF NOT EXISTS idx_productsalesanalytics_product ON ProductSalesAnalytics(ProductId);
CREATE INDEX IF NOT EXISTS idx_productsalesanalytics_date ON ProductSalesAnalytics(AnalyticsDate);
CREATE INDEX IF NOT EXISTS idx_productsalesanalytics_revenue ON ProductSalesAnalytics(Revenue);

-- Customer Analytics Indexes
CREATE INDEX IF NOT EXISTS idx_customersanalytics_customer ON CustomerAnalytics(CustomerId);
CREATE INDEX IF NOT EXISTS idx_customersanalytics_date ON CustomerAnalytics(AnalyticsDate);
CREATE INDEX IF NOT EXISTS idx_customersanalytics_spent ON CustomerAnalytics(TotalSpent);

-- Category Performance Indexes
CREATE INDEX IF NOT EXISTS idx_categoryperformance_category ON CategoryPerformance(Category);
CREATE INDEX IF NOT EXISTS idx_categoryperformance_date ON CategoryPerformance(AnalyticsDate);

-- Subscription Plans Indexes
CREATE INDEX IF NOT EXISTS idx_subscriptionplans_type ON SubscriptionPlans(PlanType);
CREATE INDEX IF NOT EXISTS idx_subscriptionplans_active ON SubscriptionPlans(IsActive);

-- Customer Subscriptions Indexes
CREATE INDEX IF NOT EXISTS idx_customersubscriptions_tenant ON CustomerSubscriptions(TenantId);
CREATE INDEX IF NOT EXISTS idx_customersubscriptions_plan ON CustomerSubscriptions(SubscriptionPlanId);
CREATE INDEX IF NOT EXISTS idx_customersubscriptions_status ON CustomerSubscriptions(Status);
CREATE INDEX IF NOT EXISTS idx_customersubscriptions_dates ON CustomerSubscriptions(StartDate, EndDate);

-- Subscription Payments Indexes
CREATE INDEX IF NOT EXISTS idx_subscriptionpayments_subscription ON SubscriptionPayments(SubscriptionId);
CREATE INDEX IF NOT EXISTS idx_subscriptionpayments_date ON SubscriptionPayments(PaymentDate);
CREATE INDEX IF NOT EXISTS idx_subscriptionpayments_status ON SubscriptionPayments(Status);

-- Sales Targets Indexes
CREATE INDEX IF NOT EXISTS idx_salestargets_tenant ON SalesTargets(TenantId);
CREATE INDEX IF NOT EXISTS idx_salestargets_type ON SalesTargets(TargetType);
CREATE INDEX IF NOT EXISTS idx_salestargets_period ON SalesTargets(TargetPeriod);
CREATE INDEX IF NOT EXISTS idx_salestargets_assigned ON SalesTargets(AssignedTo);
CREATE INDEX IF NOT EXISTS idx_salestargets_status ON SalesTargets(Status);

-- Sales Performance Indexes
CREATE INDEX IF NOT EXISTS idx_salesperformance_target ON SalesPerformance(TargetId);
CREATE INDEX IF NOT EXISTS idx_salesperformance_date ON SalesPerformance(PerformanceDate);

-- Sales Team Performance Indexes
CREATE INDEX IF NOT EXISTS idx_salesteamperformance_user ON SalesTeamPerformance(UserId);
CREATE INDEX IF NOT EXISTS idx_salesteamperformance_period ON SalesTeamPerformance(PerformancePeriod);
CREATE INDEX IF NOT EXISTS idx_salesteamperformance_ranking ON SalesTeamPerformance(Ranking);

-- User Roles Indexes (for Operations/Sales Team)
CREATE INDEX IF NOT EXISTS idx_userroles_user ON UserRoles(UserId);
CREATE INDEX IF NOT EXISTS idx_userroles_role ON UserRoles(RoleId);
CREATE INDEX IF NOT EXISTS idx_userroles_tenant ON UserRoles(TenantId);
CREATE INDEX IF NOT EXISTS idx_userroles_branch ON UserRoles(BranchId);
CREATE INDEX IF NOT EXISTS idx_userroles_active ON UserRoles(IsActive);
CREATE INDEX IF NOT EXISTS idx_salesteamperformance_period ON SalesTeamPerformance(PerformancePeriod);

-- Commission Plans Indexes
CREATE INDEX IF NOT EXISTS idx_commissionplans_type ON CommissionPlans(PlanType);
CREATE INDEX IF NOT EXISTS idx_commissionplans_active ON CommissionPlans(IsActive);

-- User Commission Plans Indexes
CREATE INDEX IF NOT EXISTS idx_usercommissionplans_user ON UserCommissionPlans(UserId);
CREATE INDEX IF NOT EXISTS idx_usercommissionplans_plan ON UserCommissionPlans(CommissionPlanId);

-- Commission Earnings Indexes
CREATE INDEX IF NOT EXISTS idx_commissionearnings_user ON CommissionEarnings(UserId);
CREATE INDEX IF NOT EXISTS idx_commissionearnings_sale ON CommissionEarnings(SaleId);
CREATE INDEX IF NOT EXISTS idx_commissionearnings_period ON CommissionEarnings(EarningPeriod);
CREATE INDEX IF NOT EXISTS idx_commissionearnings_status ON CommissionEarnings(Status);

-- Sales Territories Indexes
CREATE INDEX IF NOT EXISTS idx_salesterritories_manager ON SalesTerritories(AssignedManagerId);
CREATE INDEX IF NOT EXISTS idx_salesterritories_type ON SalesTerritories(TerritoryType);
CREATE INDEX IF NOT EXISTS idx_salesterritories_active ON SalesTerritories(IsActive);

-- Territory Assignments Indexes
CREATE INDEX IF NOT EXISTS idx_territoryassignments_territory ON TerritoryAssignments(TerritoryId);
CREATE INDEX IF NOT EXISTS idx_territoryassignments_user ON TerritoryAssignments(UserId);
CREATE INDEX IF NOT EXISTS idx_territoryassignments_dates ON TerritoryAssignments(AssignmentDate, EndDate);

-- Territory Performance Indexes
CREATE INDEX IF NOT EXISTS idx_territoryperformance_territory ON TerritoryPerformance(TerritoryId);
CREATE INDEX IF NOT EXISTS idx_territoryperformance_period ON TerritoryPerformance(PerformancePeriod);

-- ========================================
-- Triggers for Sales Operations Tables
-- ========================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply UpdatedAt trigger to Sales Operations tables
CREATE TRIGGER update_subscriptionplans_updated_at BEFORE UPDATE ON SubscriptionPlans 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customersubscriptions_updated_at BEFORE UPDATE ON CustomerSubscriptions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_salestargets_updated_at BEFORE UPDATE ON SalesTargets 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_commissionplans_updated_at BEFORE UPDATE ON CommissionPlans 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_salesterritories_updated_at BEFORE UPDATE ON SalesTerritories 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ========================================
-- Views for Sales Operations Dashboard
-- ========================================

-- View for Sales Overview
CREATE OR REPLACE VIEW SalesOverview AS
SELECT 
    ss.SummaryDate,
    ss.TotalRevenue,
    ss.TotalTransactions,
    ss.AverageTransaction,
    ss.UniqueCustomers,
    ss.CashRevenue,
    ss.CardRevenue,
    ss.MobileMoneyRevenue,
    CASE 
        WHEN LAG(ss.TotalRevenue) OVER (ORDER BY ss.SummaryDate) IS NULL THEN 0
        ELSE ROUND(((ss.TotalRevenue - LAG(ss.TotalRevenue) OVER (ORDER BY ss.SummaryDate)) / 
                   LAG(ss.TotalRevenue) OVER (ORDER BY ss.SummaryDate)) * 100, 2)
    END as RevenueGrowthPercentage
FROM SalesSummary ss
ORDER BY ss.SummaryDate DESC;

-- View for Top Performing Products
CREATE OR REPLACE VIEW TopPerformingProducts AS
SELECT 
    p.Id,
    p.Name,
    p.Category,
    COALESCE(SUM(psa.QuantitySold), 0) as TotalQuantitySold,
    COALESCE(SUM(psa.Revenue), 0) as TotalRevenue,
    COALESCE(AVG(psa.ProfitMargin), 0) as AverageProfitMargin,
    COUNT(DISTINCT psa.AnalyticsDate) as DaysSold
FROM Products p
LEFT JOIN ProductSalesAnalytics psa ON p.Id = psa.ProductId
WHERE psa.AnalyticsDate >= CURRENT_DATE - INTERVAL '30 days'
   OR psa.AnalyticsDate IS NULL
GROUP BY p.Id, p.Name, p.Category
ORDER BY TotalRevenue DESC
LIMIT 20;

-- View for Subscription Revenue
CREATE OR REPLACE VIEW SubscriptionRevenue AS
SELECT 
    sp.Name as PlanName,
    COUNT(cs.Id) as ActiveSubscriptions,
    SUM(sp.MonthlyFee) as MonthlyRecurringRevenue,
    SUM(sp.AnnualFee) as AnnualRecurringRevenue,
    AVG(sp.MonthlyFee) as AverageMonthlyFee
FROM SubscriptionPlans sp
LEFT JOIN CustomerSubscriptions cs ON sp.Id = cs.SubscriptionPlanId 
    AND cs.Status = 'Active' 
    AND cs.EndDate >= CURRENT_DATE
WHERE sp.IsActive = true
GROUP BY sp.Id, sp.Name
ORDER BY MonthlyRecurringRevenue DESC;

-- View for Sales Target Achievement
CREATE OR REPLACE VIEW SalesTargetAchievement AS
SELECT 
    st.TargetType,
    st.TargetPeriod,
    st.TargetRevenue,
    COALESCE(sp.ActualRevenue, 0) as ActualRevenue,
    CASE 
        WHEN st.TargetRevenue > 0 THEN 
            ROUND((COALESCE(sp.ActualRevenue, 0) / st.TargetRevenue) * 100, 2)
        ELSE 0
    END as AchievementPercentage,
    st.Status as TargetStatus
FROM SalesTargets st
LEFT JOIN SalesPerformance sp ON st.Id = sp.TargetId
WHERE st.Status IN ('Active', 'Achieved', 'Missed')
ORDER BY st.TargetPeriod DESC, AchievementPercentage DESC;
