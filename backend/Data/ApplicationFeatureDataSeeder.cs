using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public static class ApplicationFeatureDataSeeder
    {
        public static async Task SeedApplicationFeaturesAsync(ApplicationDbContext context)
        {
            // Check if features already exist
            var existingFeatures = await context.ApplicationFeatures.AnyAsync();
            if (existingFeatures)
            {
                return; // Features already seeded
            }

            var features = new List<ApplicationFeature>
            {
                // Inventory Management Features
                new ApplicationFeature
                {
                    Name = "Product Management",
                    Description = "Add, edit, and manage pharmaceutical products with Zambia-specific fields",
                    Category = "Inventory",
                    Module = "Inventory",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 1,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Stock Tracking",
                    Description = "Real-time stock level monitoring and automatic reorder alerts",
                    Category = "Inventory",
                    Module = "Inventory",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 2,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Batch Number Tracking",
                    Description = "Track products by batch numbers for quality control and recalls",
                    Category = "Inventory",
                    Module = "Inventory",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 3,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Expiry Date Management",
                    Description = "Monitor product expiry dates and receive alerts for expiring stock",
                    Category = "Inventory",
                    Module = "Inventory",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 4,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Supplier Management",
                    Description = "Manage supplier information and purchase orders",
                    Category = "Inventory",
                    Module = "Inventory",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 5,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Purchase Order Management",
                    Description = "Create and manage purchase orders with automated stock updates",
                    Category = "Inventory",
                    Module = "Inventory",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 6,
                    IsActive = true
                },

                // Sales Features
                new ApplicationFeature
                {
                    Name = "Point of Sale",
                    Description = "Complete POS system with barcode scanning and payment processing",
                    Category = "Sales",
                    Module = "Sales",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 7,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Customer Management",
                    Description = "Manage customer information and purchase history",
                    Category = "Sales",
                    Module = "Sales",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 8,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Invoice Generation",
                    Description = "Generate professional invoices with Zambia tax compliance",
                    Category = "Sales",
                    Module = "Sales",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 9,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Credit Note Management",
                    Description = "Issue and manage credit notes for returns and adjustments",
                    Category = "Sales",
                    Module = "Sales",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 10,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Payment Processing",
                    Description = "Multiple payment methods including mobile money and cards",
                    Category = "Sales",
                    Module = "Sales",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 11,
                    IsActive = true
                },

                // Prescription Features
                new ApplicationFeature
                {
                    Name = "Prescription Management",
                    Description = "Digital prescription creation and management",
                    Category = "Clinical",
                    Module = "Pharmacist",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 12,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Patient Records",
                    Description = "Comprehensive patient medical history and records",
                    Category = "Clinical",
                    Module = "Pharmacist",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 13,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Drug Interaction Checker",
                    Description = "Automatic detection of potential drug interactions",
                    Category = "Clinical",
                    Module = "Pharmacist",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 14,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Dosage Calculator",
                    Description = "Calculate appropriate dosages based on patient parameters",
                    Category = "Clinical",
                    Module = "Pharmacist",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 15,
                    IsActive = true
                },

                // Controlled Substances
                new ApplicationFeature
                {
                    Name = "Controlled Substance Tracking",
                    Description = "Comprehensive tracking of controlled substances per ZAMRA regulations",
                    Category = "Compliance",
                    Module = "Pharmacist",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 16,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "ZAMRA Reporting",
                    Description = "Automated generation of ZAMRA compliance reports",
                    Category = "Compliance",
                    Module = "Pharmacist",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 17,
                    IsActive = true
                },

                // Reporting Features
                new ApplicationFeature
                {
                    Name = "Sales Reports",
                    Description = "Detailed sales analytics and performance metrics",
                    Category = "Reports",
                    Module = "Sales",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 18,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Inventory Reports",
                    Description = "Comprehensive inventory analysis and stock reports",
                    Category = "Reports",
                    Module = "Inventory",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 19,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Financial Reports",
                    Description = "Profit & loss, balance sheet, and cash flow statements",
                    Category = "Reports",
                    Module = "Billing",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 20,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Custom Report Builder",
                    Description = "Create custom reports with drag-and-drop interface",
                    Category = "Reports",
                    Module = "Reports",
                    BasicPlan = false,
                    ProfessionalPlan = false,
                    EnterprisePlan = true,
                    SortOrder = 21,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Automated Report Scheduling",
                    Description = "Schedule automatic report generation and delivery",
                    Category = "Reports",
                    Module = "Reports",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 22,
                    IsActive = true
                },

                // User Management
                new ApplicationFeature
                {
                    Name = "User Management",
                    Description = "Create and manage user accounts with role-based access",
                    Category = "Administration",
                    Module = "Admin",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 23,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Role-Based Access Control",
                    Description = "Granular permissions based on user roles",
                    Category = "Administration",
                    Module = "Admin",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 24,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Activity Logging",
                    Description = "Comprehensive audit trail of all system activities",
                    Category = "Administration",
                    Module = "Admin",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 25,
                    IsActive = true
                },

                // Multi-Branch Features
                new ApplicationFeature
                {
                    Name = "Multi-Branch Management",
                    Description = "Manage multiple pharmacy branches from single account",
                    Category = "Multi-Branch",
                    Module = "Admin",
                    BasicPlan = false,
                    ProfessionalPlan = false,
                    EnterprisePlan = true,
                    SortOrder = 26,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Inter-Branch Stock Transfer",
                    Description = "Transfer stock between branches with tracking",
                    Category = "Multi-Branch",
                    Module = "Inventory",
                    BasicPlan = false,
                    ProfessionalPlan = false,
                    EnterprisePlan = true,
                    SortOrder = 27,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Consolidated Reporting",
                    Description = "Combined reports across all branches",
                    Category = "Multi-Branch",
                    Module = "Reports",
                    BasicPlan = false,
                    ProfessionalPlan = false,
                    EnterprisePlan = true,
                    SortOrder = 28,
                    IsActive = true
                },

                // API & Integration
                new ApplicationFeature
                {
                    Name = "API Access",
                    Description = "RESTful API for third-party integrations",
                    Category = "Integration",
                    Module = "API",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 29,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Custom Integrations",
                    Description = "Support for custom system integrations",
                    Category = "Integration",
                    Module = "API",
                    BasicPlan = false,
                    ProfessionalPlan = false,
                    EnterprisePlan = true,
                    SortOrder = 30,
                    IsActive = true
                },

                // Support Features
                new ApplicationFeature
                {
                    Name = "Email Support",
                    Description = "Email-based customer support during business hours",
                    Category = "Support",
                    Module = "Support",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 31,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Priority Support",
                    Description = "Priority email support with faster response times",
                    Category = "Support",
                    Module = "Support",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 32,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "24/7 Phone Support",
                    Description = "Round-the-clock phone support for critical issues",
                    Category = "Support",
                    Module = "Support",
                    BasicPlan = false,
                    ProfessionalPlan = false,
                    EnterprisePlan = true,
                    SortOrder = 33,
                    IsActive = true
                },

                // Data Management
                new ApplicationFeature
                {
                    Name = "Data Backup",
                    Description = "Automated daily data backups with 30-day retention",
                    Category = "Data Management",
                    Module = "Admin",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 34,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Data Export",
                    Description = "Export data in various formats (CSV, Excel, PDF)",
                    Category = "Data Management",
                    Module = "Admin",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 35,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Data Import",
                    Description = "Import existing data from other systems",
                    Category = "Data Management",
                    Module = "Admin",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 36,
                    IsActive = true
                },

                // Security Features
                new ApplicationFeature
                {
                    Name = "Two-Factor Authentication",
                    Description = "Enhanced security with 2FA for all users",
                    Category = "Security",
                    Module = "Admin",
                    BasicPlan = false,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 37,
                    IsActive = true
                },
                new ApplicationFeature
                {
                    Name = "Session Management",
                    Description = "Control user sessions and automatic logout",
                    Category = "Security",
                    Module = "Admin",
                    BasicPlan = true,
                    ProfessionalPlan = true,
                    EnterprisePlan = true,
                    SortOrder = 38,
                    IsActive = true
                }
            };

            await context.ApplicationFeatures.AddRangeAsync(features);
            await context.SaveChangesAsync();
        }
    }
}
