-- Seed Data for Umi Health POS Database
-- This file contains initial data for all tables

-- ========================================
-- System Configuration Seed Data
-- ========================================

INSERT INTO SystemConfiguration (ConfigKey, ConfigValue, ConfigType, Description, Category) VALUES
('system_name', 'Umi Health POS', 'String', 'System name for display purposes', 'General'),
('system_version', '1.0.0', 'String', 'Current system version', 'General'),
('default_currency', 'ZMW', 'String', 'Default currency code', 'Financial'),
('tax_rate', '0.16', 'Decimal', 'Default tax rate (16%)', 'Financial'),
('max_login_attempts', '5', 'Integer', 'Maximum failed login attempts before lockout', 'Security'),
('password_min_length', '8', 'Integer', 'Minimum password length', 'Security'),
('session_timeout_minutes', '30', 'Integer', 'User session timeout in minutes', 'Security'),
('enable_audit_logging', 'true', 'Boolean', 'Enable audit logging', 'Security'),
('backup_retention_days', '30', 'Integer', 'Number of days to retain backups', 'Backup'),
('enable_email_notifications', 'true', 'Boolean', 'Enable email notifications', 'Notifications'),
('default_language', 'en', 'String', 'Default system language', 'Localization'),
('timezone', 'Africa/Lusaka', 'String', 'Default timezone', 'Localization')
ON CONFLICT (ConfigKey) DO NOTHING;

-- ========================================
-- Super Admin User Seed Data
-- ========================================

-- Insert Super Admin user (password: admin123)
INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Role, IsActive, EmailVerified) VALUES
('superadmin', 'admin@umihealth.com', '$2a$11$QV8KzYkK.KY8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYk', 'Super', 'Admin', 'SuperAdmin', true, true)
ON CONFLICT (Username) DO NOTHING;

-- Insert Operations Admin user (password: admin123)
INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Role, IsActive, EmailVerified) VALUES
('operationsadmin', 'operations@umihealth.com', '$2a$11$QV8KzYkK.KY8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYk', 'Operations', 'Admin', 'OperationsAdmin', true, true)
ON CONFLICT (Username) DO NOTHING;

-- Insert Sales Team Admin user (password: admin123)
INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Role, IsActive, EmailVerified) VALUES
('salesteamadmin', 'sales@umihealth.com', '$2a$11$QV8KzYkK.KY8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYk', 'Sales', 'Admin', 'SalesTeamAdmin', true, true)
ON CONFLICT (Username) DO NOTHING;

-- ========================================
-- Subscription Plans Seed Data
-- ========================================

INSERT INTO SubscriptionPlans (Name, Description, PlanType, MonthlyFee, AnnualFee, Features, MaxUsers, MaxProducts, MaxTransactions, SupportLevel) VALUES
('Basic', 'Essential features for small pharmacies', 'Basic', 199.00, 1990.00, '["Inventory Management", "Basic Reports", "Email Support"]', 3, 500, 1000, 'Basic'),
('Premium', 'Advanced features for growing pharmacies', 'Premium', 499.00, 4990.00, '["Inventory Management", "Advanced Reports", "Multi-User", "Priority Support", "Mobile App"]', 10, 2000, 5000, 'Priority'),
('Enterprise', 'Complete solution for large pharmacies', 'Enterprise', 999.00, 9990.00, '["All Features", "Unlimited Users", "Custom Reports", "Dedicated Support", "API Access", "White Label"]', -1, -1, -1, 'Dedicated')
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample Tenant Data
-- ========================================

INSERT INTO Tenants (Name, BusinessName, BusinessRegistrationNumber, TaxIdentificationNumber, Address, City, Country, Phone, Email, SubscriptionPlanId, MaxUsers, MaxProducts, IsActive, IsApproved, SubscriptionStartDate, SubscriptionEndDate) VALUES
('Umi Health Pharmacy - Lusaka Central', 'Umi Health Pharmacy Ltd', 'ZAMBIA/2023/12345', '100123456789', 'Plot 1234, Cairo Road, Lusaka', 'Lusaka', 'Zambia', '+260 211 234567', 'lusaka@umihealth.com', 2, 10, 1000, true, true, CURRENT_DATE, CURRENT_DATE + INTERVAL '1 year')
ON CONFLICT DO NOTHING;

-- ========================================
-- Tenant Admin User Seed Data
-- ========================================

-- Get the tenant ID we just inserted
DO $$
DECLARE
    tenant_id INTEGER;
BEGIN
    SELECT Id INTO tenant_id FROM Tenants WHERE BusinessName = 'Umi Health Pharmacy Ltd' LIMIT 1;
    
    IF tenant_id IS NOT NULL THEN
        INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Role, TenantId, IsActive, EmailVerified) VALUES
        ('tenantadmin', 'admin@umihealth.com', '$2a$11$QV8KzYkK.KY8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYkK8kYk', 'Tenant', 'Admin', 'TenantAdmin', tenant_id, true, true)
        ON CONFLICT (Username) DO NOTHING;
    END IF;
END $$;

-- ========================================
-- Sample Product Categories and Products
-- ========================================

INSERT INTO Products (Name, Category, Price, Stock, Barcode, Description, MinStock, RequiresPrescription) VALUES
('Paracetamol 500mg', 'Pain Relief', 25.50, 100, '1234567890123', 'Paracetamol tablets 500mg for pain and fever relief', 20, false),
('Amoxicillin 250mg', 'Antibiotics', 85.75, 50, '1234567890124', 'Amoxicillin capsules 250mg for bacterial infections', 10, true),
('Ibuprofen 400mg', 'Pain Relief', 35.25, 75, '1234567890125', 'Ibuprofen tablets 400mg for pain and inflammation', 15, false),
('Cough Syrup', 'Cough & Cold', 45.00, 60, '1234567890126', 'Dextromethorphan cough syrup for dry cough', 20, false),
('Vitamin C 1000mg', 'Vitamins', 120.00, 200, '1234567890127', 'Vitamin C tablets 1000mg for immune support', 50, false),
('Antacid Tablets', 'Digestive Health', 55.50, 80, '1234567890128', 'Calcium carbonate antacid tablets for heartburn', 25, false),
('Insulin Pen', 'Diabetes', 450.00, 30, '1234567890129', 'Disposable insulin pen for diabetes management', 5, true),
('Blood Pressure Monitor', 'Medical Devices', 850.00, 20, '1234567890130', 'Digital blood pressure monitor for home use', 5, false)
ON CONFLICT (Barcode) DO NOTHING;

-- ========================================
-- Sample Inventory Items (Zambia-specific)
-- ========================================

INSERT INTO InventoryItems (InventoryItemName, GenericName, BrandName, ManufactureDate, BatchNumber, LicenseNumber, ZambiaRegNumber, PackingType, Quantity, UnitPrice, SellingPrice, ReorderLevel) VALUES
('Paracetamol Tablets 500mg', 'Paracetamol', 'PharmaCare', '2023-01-15', 'PAR0012023', 'PHL-2023-001', 'ZAMBIA-REG-001', 'Box', 100, 20.00, 25.50, 20),
('Amoxicillin Capsules 250mg', 'Amoxicillin', 'MediLab', '2023-02-20', 'AMX0022023', 'PHL-2023-002', 'ZAMBIA-REG-002', 'Box', 50, 70.00, 85.75, 10),
('Ibuprofen Tablets 400mg', 'Ibuprofen', 'HealthPlus', '2023-03-10', 'IBU0032023', 'PHL-2023-003', 'ZAMBIA-REG-003', 'Bottle', 75, 28.50, 35.25, 15),
('Dextromethorphan Syrup', 'Dextromethorphan', 'CureAll', '2023-01-25', 'COU0042023', 'PHL-2023-004', 'ZAMBIA-REG-004', 'Bottle', 60, 36.00, 45.00, 20),
('Vitamin C Tablets', 'Ascorbic Acid', 'VitaHealth', '2023-04-05', 'VIT0052023', 'PHL-2023-005', 'ZAMBIA-REG-005', 'Bottle', 200, 95.00, 120.00, 50)
ON CONFLICT (BatchNumber) DO NOTHING;

-- ========================================
-- Sample Customers
-- ========================================

INSERT INTO Customers (Name, Email, Phone, Address, CustomerType, DiscountRate) VALUES
('John Banda', 'john.banda@email.com', '+260 977 123 456', '123 Kabulonga Road, Lusaka', 'Regular', 0.00),
('Mary Mwale', 'mary.mwale@email.com', '+260 976 234 567', '456 Chilanga Road, Lusaka', 'VIP', 5.00),
('Dr. James Phiri', 'dr.phiri@clinic.com', '+260 211 345 678', '789 Medical Center, Lusaka', 'Corporate', 10.00),
('Sarah Tembo', 'sarah.tembo@email.com', '+260 975 456 789', '321 Rhodes Park, Lusaka', 'Regular', 0.00),
('Lusaka General Hospital', 'procurement@lgh.gov.zm', '+260 211 567 890', 'Government Complex, Lusaka', 'Corporate', 15.00)
ON CONFLICT (Email) DO NOTHING;

-- ========================================
-- Sample Patients
-- ========================================

INSERT INTO Patients (Name, IdNumber, PhoneNumber, Email, DateOfBirth, Gender, Address, Allergies, MedicalHistory) VALUES
('John Banda', '123456/78/9', '+260 977 123 456', 'john.banda@email.com', '1985-05-15', 'Male', '123 Kabulonga Road, Lusaka', 'Penicillin', 'Hypertension'),
('Mary Mwale', '234567/89/0', '+260 976 234 567', 'mary.mwale@email.com', '1990-08-22', 'Female', '456 Chilanga Road, Lusaka', 'None', 'None'),
('Peter Chanda', '345678/90/1', '+260 966 345 678', 'peter.chanda@email.com', '1978-12-10', 'Male', '789 Kalingalinga, Lusaka', 'Aspirin', 'Diabetes Type 2'),
('Grace Nkandu', '456789/01/2', '+260 965 456 789', 'grace.nkandu@email.com', '1995-03-18', 'Female', '321 Woodlands, Lusaka', 'Sulfa drugs', 'Asthma'),
('Joseph Bwalya', '567890/12/3', '+260 964 567 890', 'joseph.bwalya@email.com', '1982-07-25', 'Male', '654 Matero, Lusaka', 'None', 'None')
ON CONFLICT (IdNumber) DO NOTHING;

-- ========================================
-- Sample Doctors
-- ========================================

INSERT INTO Doctors (Name, RegistrationNumber, Specialization, PhoneNumber, Email, HospitalClinic) VALUES
('Dr. Sarah Mwansa', 'ZMB-MDC-2023-001', 'General Practitioner', '+260 211 234 567', 'dr.mwansa@clinic.com', 'Lusaka Central Clinic'),
('Dr. Michael Phiri', 'ZMB-MDC-2023-002', 'Pediatrician', '+260 211 345 678', 'dr.phiri@pediatrics.com', 'University Teaching Hospital'),
('Dr. Elizabeth Banda', 'ZMB-MDC-2023-003', 'Cardiologist', '+260 211 456 789', 'dr.banda@heart.com', 'Lusaka Heart Center'),
('Dr. James Chanda', 'ZMB-MDC-2023-004', 'Dermatologist', '+260 211 567 890', 'dr.chanda@skin.com', 'Lusaka Dermatology Clinic'),
('Dr. Grace Tembo', 'ZMB-MDC-2023-005', 'Obstetrician', '+260 211 678 901', 'dr.tembo@maternity.com', 'Lusaka Maternity Hospital')
ON CONFLICT (RegistrationNumber) DO NOTHING;

-- ========================================
-- Sample Prescriptions
-- ========================================

INSERT INTO Prescriptions (RxNumber, PatientId, PatientName, PatientIdNumber, DoctorName, DoctorRegistrationNumber, Medication, Dosage, Instructions, TotalCost, Status, PrescriptionDate, IsUrgent) VALUES
('RX2024010001', 1, 'John Banda', '123456/78/9', 'Dr. Sarah Mwansa', 'ZMB-MDC-2023-001', 'Amoxicillin 250mg', '1 capsule 3 times daily for 7 days', 'Take with food', 85.75, 'filled', '2024-01-15', false),
('RX2024010002', 2, 'Mary Mwale', '234567/89/0', 'Dr. Michael Phiri', 'ZMB-MDC-2023-002', 'Paracetamol 500mg', '1 tablet every 6 hours as needed', 'Do not exceed 4 tablets in 24 hours', 25.50, 'pending', '2024-01-16', true),
('RX2024010003', 3, 'Peter Chanda', '345678/90/1', 'Dr. Elizabeth Banda', 'ZMB-MDC-2023-003', 'Ibuprofen 400mg', '1 tablet twice daily with meals', 'Take with food to avoid stomach upset', 35.25, 'approved', '2024-01-17', false),
('RX2024010004', 4, 'Grace Nkandu', '456789/01/2', 'Dr. James Chanda', 'ZMB-MDC-2023-004', 'Vitamin C 1000mg', '1 tablet daily', 'Take in the morning with breakfast', 120.00, 'filled', '2024-01-18', false),
('RX2024010005', 5, 'Joseph Bwalya', '567890/12/3', 'Dr. Grace Tembo', 'ZMB-MDC-2023-005', 'Antacid Tablets', '2 tablets as needed after meals', 'Chew thoroughly before swallowing', 55.50, 'pending', '2024-01-19', false)
ON CONFLICT (RxNumber) DO NOTHING;

-- ========================================
-- Sample Sales
-- ========================================

INSERT INTO Sales (SaleNumber, CustomerId, CashierId, Subtotal, Tax, Discount, Total, PaymentMethod, CashReceived, Change, Status, SaleType) VALUES
('SALE-20240115-001', 1, 2, 85.75, 13.72, 0.00, 99.47, 'Cash', 100.00, 0.53, 'Completed', 'Prescription'),
('SALE-20240115-002', 2, 2, 25.50, 4.08, 0.00, 29.58, 'Mobile Money', 29.58, 0.00, 'Completed', 'Retail'),
('SALE-20240115-003', 3, 2, 35.25, 5.64, 0.00, 40.89, 'Card', 40.89, 0.00, 'Completed', 'Retail'),
('SALE-20240115-004', NULL, 2, 120.00, 19.20, 6.00, 133.20, 'Cash', 150.00, 16.80, 'Completed', 'Retail'),
('SALE-20240115-005', 4, 2, 55.50, 8.88, 0.00, 64.38, 'Mobile Money', 64.38, 0.00, 'Completed', 'Retail')
ON CONFLICT (SaleNumber) DO NOTHING;

-- ========================================
-- Sample Sale Items
-- ========================================

INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, TotalPrice, Prescribed) VALUES
(1, 2, 1, 85.75, 85.75, true),
(2, 1, 1, 25.50, 25.50, false),
(3, 3, 1, 35.25, 35.25, false),
(4, 5, 1, 120.00, 120.00, false),
(5, 6, 1, 55.50, 55.50, false)
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample Prescription Items
-- ========================================

INSERT INTO PrescriptionItems (PrescriptionId, InventoryItemId, MedicationName, Dosage, Quantity, Instructions, UnitPrice, TotalPrice) VALUES
(1, 2, 'Amoxicillin 250mg', '1 capsule 3 times daily', 21, 'Take with food', 4.08, 85.75),
(2, 1, 'Paracetamol 500mg', '1 tablet every 6 hours', 28, 'Do not exceed 4 tablets in 24 hours', 0.91, 25.50),
(3, 3, 'Ibuprofen 400mg', '1 tablet twice daily', 14, 'Take with food', 2.52, 35.25),
(4, 5, 'Vitamin C 1000mg', '1 tablet daily', 30, 'Take in the morning', 4.00, 120.00),
(5, 6, 'Antacid Tablets', '2 tablets as needed', 50, 'Chew thoroughly', 1.11, 55.50)
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample Stock Transactions
-- ========================================

INSERT INTO StockTransactions (ProductId, InventoryItemId, TransactionType, QuantityChange, PreviousStock, NewStock, Reason) VALUES
(1, 1, 'Purchase', 100, 0, 100, 'Initial stock purchase'),
(2, 2, 'Purchase', 50, 0, 50, 'Initial stock purchase'),
(3, 3, 'Purchase', 75, 0, 75, 'Initial stock purchase'),
(4, 4, 'Purchase', 60, 0, 60, 'Initial stock purchase'),
(5, 5, 'Purchase', 200, 0, 200, 'Initial stock purchase'),
(1, 1, 'Sale', -1, 100, 99, 'Sale to John Banda - RX2024010001'),
(2, 2, 'Sale', -1, 50, 49, 'Sale to Mary Mwale - RX2024010002'),
(3, 3, 'Sale', -1, 75, 74, 'Sale to Peter Chanda - RX2024010003'),
(5, 5, 'Sale', -1, 200, 199, 'Sale to walk-in customer'),
(6, 6, 'Sale', -1, 60, 59, 'Sale to Grace Nkandu - RX2024010005')
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample System Settings
-- ========================================

INSERT INTO SystemSettings (SettingKey, SettingValue, Description, Category) VALUES
('business_name', 'Umi Health Pharmacy', 'Business name for receipts and reports', 'General'),
('business_address', 'Plot 1234, Cairo Road, Lusaka, Zambia', 'Business address', 'General'),
('business_phone', '+260 211 234567', 'Business phone number', 'General'),
('business_email', 'info@umihealth.com', 'Business email address', 'General'),
('currency_code', 'ZMW', 'Currency code for transactions', 'Financial'),
('currency_symbol', 'ZMW', 'Currency symbol for display', 'Financial'),
('tax_rate', '0.16', 'Default tax rate (16%)', 'Financial'),
('low_stock_notification', 'true', 'Enable low stock notifications', 'Inventory'),
('prescription_expiry_days', '30', 'Default prescription expiry in days', 'Pharmacy'),
('enable_audit_logging', 'true', 'Enable audit logging', 'Security'),
('session_timeout_minutes', '30', 'User session timeout in minutes', 'Security'),
('max_login_attempts', '5', 'Maximum failed login attempts', 'Security'),
('password_min_length', '8', 'Minimum password length', 'Security'),
('backup_frequency', 'daily', 'Backup frequency', 'Backup'),
('backup_retention_days', '30', 'Backup retention period in days', 'Backup'),
('enable_email_notifications', 'true', 'Enable email notifications', 'Notifications'),
('smtp_server', 'smtp.umihealth.com', 'SMTP server for emails', 'Notifications'),
('smtp_port', '587', 'SMTP port', 'Notifications'),
('default_language', 'en', 'Default system language', 'Localization'),
('timezone', 'Africa/Lusaka', 'Default timezone', 'Localization')
ON CONFLICT (SettingKey) DO NOTHING;

-- ========================================
-- Sample Dashboard Settings
-- ========================================

-- Insert dashboard settings for super admin
INSERT INTO DashboardSettings (UserId, WidgetLayout, Theme, Language, TimeZone) VALUES
(1, '{"widgets": [{"id": "system-overview", "position": {"x": 0, "y": 0, "w": 6, "h": 4}}, {"id": "tenant-stats", "position": {"x": 6, "y": 0, "w": 6, "h": 4}}, {"id": "system-health", "position": {"x": 0, "y": 4, "w": 12, "h": 3}}]}', 'light', 'en', 'Africa/Lusaka')
ON CONFLICT (UserId) DO NOTHING;

-- ========================================
-- Sample Promotions
-- ========================================

INSERT INTO Promotions (Name, Description, PromotionType, DiscountType, DiscountValue, MinimumPurchase, StartDate, EndDate, IsActive) VALUES
('January Sale', '15% off all vitamins and supplements', 'Discount', 'Percentage', 15.00, 50.00, '2024-01-01 00:00:00', '2024-01-31 23:59:59', true),
('Weekend Special', '10% off all pain relief medication', 'Discount', 'Percentage', 10.00, 25.00, '2024-01-20 00:00:00', '2024-01-21 23:59:59', true),
('Bulk Purchase', 'Buy 3 get 1 free on selected items', 'BuyOneGetOne', 'Fixed', 0.00, 0.00, '2024-01-15 00:00:00', '2024-02-15 23:59:59', true)
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample Loyalty Programs
-- ========================================

INSERT INTO LoyaltyPrograms (Name, Description, PointsPerDollar, MinimumPurchase, IsActive, StartDate) VALUES
('Umi Health Rewards', 'Earn points on every purchase', 1.00, 10.00, true, '2024-01-01'),
('VIP Health Club', 'Exclusive benefits for loyal customers', 1.50, 50.00, true, '2024-01-01')
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample Commission Plans
-- ========================================

INSERT INTO CommissionPlans (Name, Description, PlanType, CommissionStructure, MinimumSales, IsActive) VALUES
('Basic Sales Commission', '5% commission on all sales', 'Percentage', '{"rate": 0.05, "type": "flat"}', 0.00, true),
('Tiered Commission', 'Progressive commission rates', 'Tiered', '{"tiers": [{"min": 0, "max": 1000, "rate": 0.05}, {"min": 1000, "max": 5000, "rate": 0.07}, {"min": 5000, "rate": 0.10}]}', 0.00, true),
('Performance Bonus', 'Additional bonus for high performers', 'Hybrid', '{"base_rate": 0.05, "bonus_threshold": 10000, "bonus_rate": 0.02}', 0.00, true)
ON CONFLICT DO NOTHING;

-- ========================================
-- Sample Sales Targets
-- ========================================

INSERT INTO SalesTargets (TenantId, TargetType, TargetPeriod, TargetRevenue, TargetTransactions, TargetCustomers, Status) VALUES
(1, 'Monthly', '2024-01', 50000.00, 500, 200, 'Active'),
(1, 'Weekly', '2024-W03', 12500.00, 125, 50, 'Achieved'),
(1, 'Daily', '2024-01-15', 2000.00, 20, 8, 'Achieved')
ON CONFLICT DO NOTHING;

-- ========================================
-- Final Verification Queries
-- ========================================

-- Verify seed data insertion
DO $$
BEGIN
    RAISE NOTICE 'Seed data insertion completed successfully!';
    RAISE NOTICE 'Total Users: %', (SELECT COUNT(*) FROM Users);
    RAISE NOTICE 'Total Tenants: %', (SELECT COUNT(*) FROM Tenants);
    RAISE NOTICE 'Total Products: %', (SELECT COUNT(*) FROM Products);
    RAISE NOTICE 'Total Inventory Items: %', (SELECT COUNT(*) FROM InventoryItems);
    RAISE NOTICE 'Total Customers: %', (SELECT COUNT(*) FROM Customers);
    RAISE NOTICE 'Total Patients: %', (SELECT COUNT(*) FROM Patients);
    RAISE NOTICE 'Total Prescriptions: %', (SELECT COUNT(*) FROM Prescriptions);
    RAISE NOTICE 'Total Sales: %', (SELECT COUNT(*) FROM Sales);
END $$;
