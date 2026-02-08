-- Umi Health POS - PostgreSQL Database Schema
-- Run this script to create the database schema

-- Create Products table
CREATE TABLE IF NOT EXISTS "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Category" VARCHAR(100),
    "Price" DECIMAL(10,2) NOT NULL,
    "Stock" INTEGER NOT NULL DEFAULT 0,
    "Barcode" VARCHAR(50) UNIQUE,
    "Description" VARCHAR(500),
    "MinStock" INTEGER NOT NULL DEFAULT 5,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create Customers table
CREATE TABLE IF NOT EXISTS "Customers" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) UNIQUE,
    "Phone" VARCHAR(20),
    "Address" VARCHAR(200),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create Sales table
CREATE TABLE IF NOT EXISTS "Sales" (
    "Id" SERIAL PRIMARY KEY,
    "CustomerId" INTEGER NOT NULL REFERENCES "Customers"("Id"),
    "Subtotal" DECIMAL(10,2) NOT NULL,
    "Tax" DECIMAL(10,2) NOT NULL,
    "Total" DECIMAL(10,2) NOT NULL,
    "PaymentMethod" VARCHAR(20) NOT NULL,
    "CashReceived" DECIMAL(10,2) NOT NULL,
    "Change" DECIMAL(10,2) NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Completed',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create SaleItems table
CREATE TABLE IF NOT EXISTS "SaleItems" (
    "Id" SERIAL PRIMARY KEY,
    "SaleId" INTEGER NOT NULL REFERENCES "Sales"("Id") ON DELETE CASCADE,
    "ProductId" INTEGER NOT NULL REFERENCES "Products"("Id"),
    "UnitPrice" DECIMAL(10,2) NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "TotalPrice" DECIMAL(10,2) NOT NULL
);

-- Create StockTransactions table
CREATE TABLE IF NOT EXISTS "StockTransactions" (
    "Id" SERIAL PRIMARY KEY,
    "ProductId" INTEGER NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "TransactionType" VARCHAR(50) NOT NULL,
    "QuantityChange" INTEGER NOT NULL,
    "PreviousStock" INTEGER NOT NULL,
    "NewStock" INTEGER NOT NULL,
    "Reason" VARCHAR(200),
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS "IX_Products_Barcode" ON "Products"("Barcode");
CREATE INDEX IF NOT EXISTS "IX_Products_Category" ON "Products"("Category");
CREATE INDEX IF NOT EXISTS "IX_Customers_Email" ON "Customers"("Email");
CREATE INDEX IF NOT EXISTS "IX_SaleItems_ProductId" ON "SaleItems"("ProductId");
CREATE INDEX IF NOT EXISTS "IX_SaleItems_SaleId" ON "SaleItems"("SaleId");
CREATE INDEX IF NOT EXISTS "IX_Sales_CustomerId" ON "Sales"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_StockTransactions_ProductId_CreatedAt" ON "StockTransactions"("ProductId", "CreatedAt");

-- Insert initial data
INSERT INTO "Products" ("Id", "Name", "Category", "Price", "Stock", "Barcode", "Description", "MinStock", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(1, 'Paracetamol 500mg', 'Medications', 5.99, 50, '1234567890', 'Pain relief medication', 10, TRUE, NOW(), NOW()),
(2, 'Ibuprofen 400mg', 'Medications', 7.49, 30, '1234567891', 'Anti-inflammatory medication', 10, TRUE, NOW(), NOW()),
(3, 'Face Masks', 'Medical Supplies', 12.99, 100, '1234567892', 'Disposable face masks', 20, TRUE, NOW(), NOW()),
(4, 'Hand Sanitizer', 'Personal Care', 4.99, 75, '1234567893', 'Alcohol-based hand sanitizer', 15, TRUE, NOW(), NOW()),
(5, 'Vitamin C 1000mg', 'Vitamins', 9.99, 60, '1234567894', 'Vitamin C supplement', 15, TRUE, NOW(), NOW());

INSERT INTO "Customers" ("Id", "Name", "Email", "Phone", "Address", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(1, 'Walk-in Customer', 'walkin@umihealth.com', '000-000-0000', 'Pharmacy Counter', TRUE, NOW(), NOW()),
(2, 'John Doe', 'john.doe@example.com', '123-456-7890', '123 Main St, Lusaka', TRUE, NOW(), NOW()),
(3, 'Jane Smith', 'jane.smith@example.com', '098-765-4321', '456 Oak Ave, Lusaka', TRUE, NOW(), NOW());
