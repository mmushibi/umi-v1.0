-- Create Umi Health POS Database Schema

-- Create Tenants table
CREATE TABLE IF NOT EXISTS "Tenants" (
    "Id" SERIAL PRIMARY KEY,
    "TenantId" VARCHAR(6) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Status" VARCHAR(20) DEFAULT 'Active',
    "PharmacyName" VARCHAR(200),
    "AdminName" VARCHAR(100),
    "PhoneNumber" VARCHAR(20),
    "Email" VARCHAR(100),
    "Address" TEXT,
    "LicenseNumber" VARCHAR(100),
    "ZambiaRegNumber" VARCHAR(100),
    "SubscriptionPlan" INTEGER,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create Products table
CREATE TABLE IF NOT EXISTS "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Category" VARCHAR(100),
    "Description" TEXT,
    "UnitPrice" NUMERIC(10,2) NOT NULL,
    "SellingPrice" NUMERIC(10,2) NOT NULL,
    "Stock" INTEGER NOT NULL DEFAULT 0,
    "TenantId" VARCHAR(6) NOT NULL
);

-- Create Customers table
CREATE TABLE IF NOT EXISTS "Customers" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100),
    "Phone" VARCHAR(20),
    "Address" VARCHAR(200),
    "TenantId" VARCHAR(6) NOT NULL
);

-- Create Sales table
CREATE TABLE IF NOT EXISTS "Sales" (
    "Id" SERIAL PRIMARY KEY,
    "ReceiptNumber" VARCHAR(50) NOT NULL UNIQUE,
    "Subtotal" NUMERIC(10,2) NOT NULL,
    "Tax" NUMERIC(10,2) NOT NULL,
    "Total" NUMERIC(10,2) NOT NULL,
    "CashReceived" NUMERIC(10,2) NOT NULL,
    "Change" NUMERIC(10,2) NOT NULL,
    "PaymentMethod" VARCHAR(20) NOT NULL,
    "PaymentDetails" VARCHAR(100),
    "Status" VARCHAR(20) NOT NULL,
    "RefundReason" TEXT,
    "CustomerId" INTEGER,
    "TenantId" VARCHAR(6) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create SaleItems table
CREATE TABLE IF NOT EXISTS "SaleItems" (
    "Id" SERIAL PRIMARY KEY,
    "SaleId" INTEGER NOT NULL,
    "ProductId" INTEGER NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitPrice" NUMERIC(10,2) NOT NULL,
    "TotalPrice" NUMERIC(10,2) NOT NULL,
    CONSTRAINT "FK_SaleItems_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SaleItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);

-- Create InventoryItems table
CREATE TABLE IF NOT EXISTS "InventoryItems" (
    "Id" SERIAL PRIMARY KEY,
    "InventoryItemName" VARCHAR(200) NOT NULL,
    "GenericName" VARCHAR(200) NOT NULL,
    "BrandName" VARCHAR(200) NOT NULL,
    "BatchNumber" VARCHAR(100) NOT NULL,
    "LicenseNumber" VARCHAR(100),
    "ZambiaRegNumber" VARCHAR(100),
    "PackingType" VARCHAR(50) NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitPrice" NUMERIC(10,2) NOT NULL,
    "SellingPrice" NUMERIC(10,2) NOT NULL,
    "BranchId" INTEGER,
    "TenantId" VARCHAR(6) NOT NULL,
    "ManufactureDate" DATE
);

-- Insert sample data for testing
INSERT INTO "Tenants" ("TenantId", "Name", "Status") VALUES ('TEN001', 'Default Pharmacy', 'Active');
INSERT INTO "Products" ("Name", "Category", "UnitPrice", "SellingPrice", "Stock", "TenantId") 
VALUES ('Paracetamol 500mg', 'Medicines', 10.50, 15.00, 100, 'TEN001');
