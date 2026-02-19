using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public class HelpTrainingDataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HelpTrainingDataSeeder> _logger;

        public HelpTrainingDataSeeder(ApplicationDbContext context, ILogger<HelpTrainingDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedHelpTrainingDataAsync()
        {
            try
            {
                // Seed Help Categories
                if (!await _context.HelpCategories.AnyAsync())
                {
                    var categories = new[]
                    {
                        new HelpCategory
                        {
                            CategoryId = "getting-started",
                            Name = "Getting Started",
                            Description = "Basic setup and initial configuration",
                            Icon = "fas fa-rocket",
                            Order = 1,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "inventory-management",
                            Name = "Inventory Management",
                            Description = "Managing products, stock, and suppliers",
                            Icon = "fas fa-boxes",
                            Order = 2,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "prescriptions",
                            Name = "Prescriptions",
                            Description = "Creating and managing prescriptions",
                            Icon = "fas fa-pills",
                            Order = 3,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "billing-payments",
                            Name = "Billing & Payments",
                            Description = "Processing sales and managing payments",
                            Icon = "fas fa-cash-register",
                            Order = 4,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "reports",
                            Name = "Reports",
                            Description = "Generating and analyzing reports",
                            Icon = "fas fa-chart-bar",
                            Order = 5,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "user-management",
                            Name = "User Management",
                            Description = "Managing users and permissions",
                            Icon = "fas fa-users",
                            Order = 6,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "compliance",
                            Name = "Compliance & Regulations",
                            Description = "Zambia pharmacy regulations and compliance",
                            Icon = "fas fa-shield-alt",
                            Order = 7,
                            Status = "Active"
                        },
                        new HelpCategory
                        {
                            CategoryId = "troubleshooting",
                            Name = "Troubleshooting",
                            Description = "Common issues and solutions",
                            Icon = "fas fa-tools",
                            Order = 8,
                            Status = "Active"
                        }
                    };

                    await _context.HelpCategories.AddRangeAsync(categories);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Seeded {Count} help categories", categories.Length);
                }

                // Seed Help Articles
                if (!await _context.HelpArticles.AnyAsync())
                {
                    var articles = new[]
                    {
                        new HelpArticle
                        {
                            ArticleId = "quick-start-guide",
                            Title = "Quick Start Guide",
                            Description = "Get up and running with UMI Health POS in minutes",
                            Content = @"<h2>Welcome to UMI Health POS</h2>
<p>This comprehensive guide will help you get started with the UMI Health POS system quickly and efficiently.</p>
<h3>Step 1: System Setup</h3>
<p>Ensure your system meets the minimum requirements and all dependencies are installed.</p>
<h3>Step 2: User Account</h3>
<p>Create your user account and configure your profile settings.</p>
<h3>Step 3: Initial Configuration</h3>
<p>Set up your pharmacy information, payment methods, and basic settings.</p>
<h3>Step 4: Add Products</h3>
<p>Import or manually add your product inventory to the system.</p>
<p>For detailed assistance, please refer to our video tutorials or contact support.</p>",
                            CategoryId = "getting-started",
                            Order = 1,
                            ReadingTime = "5 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "user-account-setup",
                            Title = "Setting Up Your User Account",
                            Description = "Configure your profile and preferences",
                            Content = @"<h2>User Account Configuration</h2>
<p>Your user account is your gateway to all UMI Health POS features.</p>
<h3>Profile Information</h3>
<p>Keep your profile information up to date for better system personalization.</p>
<h3>Security Settings</h3>
<p>Configure your password, two-factor authentication, and security preferences.</p>
<h3>Notification Preferences</h3>
<p>Choose how you want to receive important updates and alerts.</p>",
                            CategoryId = "getting-started",
                            Order = 2,
                            ReadingTime = "3 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "adding-products",
                            Title = "Adding Products to Inventory",
                            Description = "Learn how to add and manage products",
                            Content = @"<h2>Product Management</h2>
<p>Effective inventory management starts with proper product setup.</p>
<h3>Manual Product Entry</h3>
<p>Step-by-step guide to adding products individually.</p>
<h3>Bulk Import</h3>
<p>Import multiple products using CSV files.</p>
<h3>Product Categories</h3>
<p>Organize your products with appropriate categorization.</p>
<h3>Pricing and Stock Levels</h3>
<p>Set up pricing structures and maintain optimal stock levels.</p>",
                            CategoryId = "inventory-management",
                            Order = 1,
                            ReadingTime = "7 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "stock-management",
                            Title = "Stock Level Management",
                            Description = "Monitor and maintain optimal stock levels",
                            Content = @"<h2>Stock Management Best Practices</h2>
<p>Maintaining proper stock levels is crucial for pharmacy operations.</p>
<h3>Stock Monitoring</h3>
<p>Real-time stock level tracking and alerts.</p>
<h3>Reorder Points</h3>
<p>Set up automatic reorder points to prevent stockouts.</p>
<h3>Stock Adjustments</h3>
<p>Handle stock returns, damages, and adjustments properly.</p>",
                            CategoryId = "inventory-management",
                            Order = 2,
                            ReadingTime = "6 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "creating-prescriptions",
                            Title = "Creating Prescriptions",
                            Description = "Step-by-step guide to prescription creation",
                            Content = @"<h2>Prescription Management</h2>
<p>Create and manage prescriptions efficiently with our intuitive interface.</p>
<h3>Patient Search</h3>
<p>Quickly find and select patients for prescription creation.</p>
<h3>Medication Selection</h3>
<p>Search and add medications from your inventory.</p>
<h3>Dosage and Instructions</h3>
<p>Set proper dosages and provide clear instructions.</p>
<h3>Prescription Validation</h3>
<p>Ensure prescription accuracy with built-in validation checks.</p>",
                            CategoryId = "prescriptions",
                            Order = 1,
                            ReadingTime = "8 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "payment-processing",
                            Title = "Payment Processing",
                            Description = "Handle various payment methods and transactions",
                            Content = @"<h2>Payment Processing Guide</h2>
<p>Accept and process payments through multiple channels.</p>
<h3>Cash Payments</h3>
<p>Process cash transactions with proper change calculation.</p>
<h3>Mobile Money</h3>
<p>Integrate with Zambian mobile money providers.</p>
<h3>Card Payments</h3>
<p>Accept debit and credit card payments.</p>
<h3>Insurance Claims</h3>
<p>Process insurance claims and reimbursements.</p>",
                            CategoryId = "billing-payments",
                            Order = 1,
                            ReadingTime = "6 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "sales-reports",
                            Title = "Sales Reports Analysis",
                            Description = "Generate and analyze sales performance reports",
                            Content = @"<h2>Sales Analytics</h2>
<p>Comprehensive reporting to track your business performance.</p>
<h3>Daily Sales Reports</h3>
<p>Monitor daily sales performance and trends.</p>
<h3>Product Performance</h3>
<p>Analyze top-selling products and inventory turnover.</p>
<h3>Customer Analytics</h3>
<p>Track customer behavior and purchase patterns.</p>",
                            CategoryId = "reports",
                            Order = 1,
                            ReadingTime = "5 min read",
                            Status = "Published"
                        },
                        new HelpArticle
                        {
                            ArticleId = "zambia-compliance",
                            Title = "Zambia Pharmacy Compliance",
                            Description = "Understanding Zambian pharmacy regulations",
                            Content = @"<h2>Zambia Regulatory Compliance</h2>
<p>Stay compliant with Zambia's pharmacy regulations and requirements.</p>
<h3>Pharmacy Board of Zambia</h3>
<p>Key regulations and compliance requirements.</p>
<h3>Controlled Substances</h3>
<p>Proper handling and documentation of controlled substances.</p>
<h3Record Keeping</h3>
<p>Maintain proper records as required by law.</p>",
                            CategoryId = "compliance",
                            Order = 1,
                            ReadingTime = "10 min read",
                            Status = "Published"
                        }
                    };

                    await _context.HelpArticles.AddRangeAsync(articles);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Seeded {Count} help articles", articles.Length);
                }

                _logger.LogInformation("Help & Training data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Help & Training data");
                throw;
            }
        }
    }
}
