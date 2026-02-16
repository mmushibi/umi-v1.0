using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

            await new SubscriptionDataSeeder(context, logger).SeedSubscriptionDataAsync();

            try
            {
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // Seed Branches
                if (!await context.Branches.AnyAsync())
                {
                    var branches = new[]
                    {
                        new Branch
                        {
                            Name = "Umi Health Main Branch",
                            Address = "123 Cairo Road, Lusaka",
                            Region = "Lusaka Province",
                            Phone = "+260211234567",
                            Email = "main@umihealth.com",
                            ManagerName = "Dr. Sarah Mwansa",
                            ManagerPhone = "+260976543210",
                            OperatingHours = "08:00-18:00",
                            Status = "active",
                            MonthlyRevenue = 150000.00m,
                            StaffCount = 15,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Branch
                        {
                            Name = "Umi Health Kitwe Branch",
                            Address = "456 Obote Avenue, Kitwe",
                            Region = "Copperbelt Province",
                            Phone = "+260212345678",
                            Email = "kitwe@umihealth.com",
                            ManagerName = "Dr. James Banda",
                            ManagerPhone = "+260976543211",
                            OperatingHours = "08:00-18:00",
                            Status = "active",
                            MonthlyRevenue = 120000.00m,
                            StaffCount = 12,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Branches.AddRangeAsync(branches);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} branches", branches.Length);
                }

                // Seed Users
                if (!await context.Users.AnyAsync())
                {
                    var branches = await context.Branches.ToListAsync();
                    var users = new[]
                    {
                        new User
                        {
                            Id = "admin-user-001",
                            FirstName = "Admin",
                            LastName = "User",
                            Email = "admin@umihealth.com",
                            NormalizedEmail = "ADMIN@UMIHEALTH.COM",
                            PhoneNumber = "+260976543212",
                            Role = "TenantAdmin",
                            Department = "Management",
                            PasswordHash = "AQAAAAEAACcQAAAAEKqgkTvtFvYFGj2Z8qZ7vK9d8X7r5J3gH2f9L1kN8oM=", // Hashed "Admin123!"
                            BranchId = branches[0].Id,
                            Address = "123 Admin Street, Lusaka",
                            City = "Lusaka",
                            Country = "Zambia",
                            LicenseNumber = "ZMP-2023-001",
                            RegistrationNumber = "ZMR-2023-001",
                            IsActive = true,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true,
                            Status = "active",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new User
                        {
                            Id = "pharmacist-001",
                            FirstName = "Grace",
                            LastName = "Chilufya",
                            Email = "grace@umihealth.com",
                            NormalizedEmail = "GRACE@UMIHEALTH.COM",
                            PhoneNumber = "+260976543213",
                            Role = "Pharmacist",
                            Department = "Pharmacy",
                            PasswordHash = "AQAAAAEAACcQAAAAEKqgkTvtFvYFGj2Z8qZ7vK9d8X7r5J3gH2f9L1kN8oM=", // Hashed "Admin123!"
                            BranchId = branches[0].Id,
                            Address = "456 Pharmacist Street, Lusaka",
                            City = "Lusaka",
                            Country = "Zambia",
                            LicenseNumber = "ZMP-2023-002",
                            RegistrationNumber = "ZMR-2023-002",
                            IsActive = true,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true,
                            Status = "active",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new User
                        {
                            Id = "cashier-001",
                            FirstName = "John",
                            LastName = "Banda",
                            Email = "john@umihealth.com",
                            NormalizedEmail = "JOHN@UMIHEALTH.COM",
                            PhoneNumber = "+260976543214",
                            Role = "Cashier",
                            Department = "Sales",
                            PasswordHash = "AQAAAAEAACcQAAAAEKqgkTvtFvYFGj2Z8qZ7vK9d8X7r5J3gH2f9L1kN8oM=", // Hashed "Admin123!"
                            BranchId = branches[0].Id,
                            Address = "789 Cashier Street, Lusaka",
                            City = "Lusaka",
                            Country = "Zambia",
                            IsActive = true,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true,
                            Status = "active",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Users.AddRangeAsync(users);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} users", users.Length);
                }

                // Seed UserBranch relationships
                if (!await context.UserBranches.AnyAsync())
                {
                    var users = await context.Users.ToListAsync();
                    var branches = await context.Branches.ToListAsync();

                    var userBranches = new[]
                    {
                        new UserBranch
                        {
                            UserId = users[0].Id, // Admin user
                            BranchId = branches[0].Id, // Main branch
                            UserRole = "TenantAdmin",
                            Permission = "admin",
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        },
                        new UserBranch
                        {
                            UserId = users[0].Id, // Admin user
                            BranchId = branches[1].Id, // Kitwe branch
                            UserRole = "TenantAdmin",
                            Permission = "admin",
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        },
                        new UserBranch
                        {
                            UserId = users[1].Id, // Pharmacist user
                            BranchId = branches[0].Id, // Main branch
                            UserRole = "Pharmacist",
                            Permission = "write",
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        },
                        new UserBranch
                        {
                            UserId = users[2].Id, // Cashier user
                            BranchId = branches[0].Id, // Main branch
                            UserRole = "Cashier",
                            Permission = "read",
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        }
                    };

                    await context.UserBranches.AddRangeAsync(userBranches);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} user-branch relationships", userBranches.Length);
                }

                // Seed Inventory Items
                if (!await context.InventoryItems.AnyAsync())
                {
                    var branches = await context.Branches.ToListAsync();
                    var inventoryItems = new[]
                    {
                        new InventoryItem
                        {
                            InventoryItemName = "Paracetamol 500mg",
                            GenericName = "Paracetamol",
                            BrandName = "Panadol",
                            ManufactureDate = new DateTime(2023, 1, 15),
                            BatchNumber = "PAN-2023-001",
                            LicenseNumber = "ZMP-LIC-001",
                            ZambiaRegNumber = "ZMR-REG-001",
                            PackingType = "Box",
                            Quantity = 100,
                            UnitPrice = 2.50m,
                            SellingPrice = 5.00m,
                            ReorderLevel = 20,
                            BranchId = branches[0].Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new InventoryItem
                        {
                            InventoryItemName = "Amoxicillin 250mg",
                            GenericName = "Amoxicillin",
                            BrandName = "Amoxil",
                            ManufactureDate = new DateTime(2023, 2, 20),
                            BatchNumber = "AMX-2023-001",
                            LicenseNumber = "ZMP-LIC-002",
                            ZambiaRegNumber = "ZMR-REG-002",
                            PackingType = "Bottle",
                            Quantity = 50,
                            UnitPrice = 15.00m,
                            SellingPrice = 25.00m,
                            ReorderLevel = 15,
                            BranchId = branches[0].Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new InventoryItem
                        {
                            InventoryItemName = "Vitamin C 500mg",
                            GenericName = "Ascorbic Acid",
                            BrandName = "Cevit",
                            ManufactureDate = new DateTime(2023, 3, 10),
                            BatchNumber = "VIT-2023-001",
                            LicenseNumber = "ZMP-LIC-003",
                            ZambiaRegNumber = "ZMR-REG-003",
                            PackingType = "Packet",
                            Quantity = 200,
                            UnitPrice = 1.00m,
                            SellingPrice = 2.50m,
                            ReorderLevel = 50,
                            BranchId = branches[1].Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.InventoryItems.AddRangeAsync(inventoryItems);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} inventory items", inventoryItems.Length);
                }

                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }
    }
}
