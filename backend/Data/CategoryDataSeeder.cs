using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using System;
using System.Threading.Tasks;

namespace UmiHealthPOS.Data
{
    public class CategoryDataSeeder
    {
        public static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            // Check if categories already exist
            if (await context.Categories.AnyAsync())
            {
                return; // Database has been seeded
            }

            var categories = new[]
            {
                new Category
                {
                    Name = "Prescription Medications",
                    Code = "RX",
                    Description = "Medications requiring a valid prescription from licensed healthcare provider",
                    Status = "Active",
                    ColorClass = "bg-blue-100 text-blue-600",
                    ItemCount = 1247,
                    TenantUsage = 89,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Over-the-Counter",
                    Code = "OTC",
                    Description = "Non-prescription medications available for direct purchase",
                    Status = "Active",
                    ColorClass = "bg-green-100 text-green-600",
                    ItemCount = 456,
                    TenantUsage = 87,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Controlled Substances",
                    Code = "CS",
                    Description = "Medications with potential for abuse or dependence requiring special handling",
                    Status = "Active",
                    ColorClass = "bg-red-100 text-red-600",
                    ItemCount = 89,
                    TenantUsage = 45,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Schedule II Drugs",
                    Code = "S2",
                    Description = "High potential for abuse, with use potentially leading to severe dependence",
                    Status = "Active",
                    ColorClass = "bg-purple-100 text-purple-600",
                    ItemCount = 34,
                    TenantUsage = 23,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Medical Devices",
                    Code = "MD",
                    Description = "Medical equipment and devices for patient care and monitoring",
                    Status = "Active",
                    ColorClass = "bg-yellow-accent text-dark-bg",
                    ItemCount = 234,
                    TenantUsage = 67,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Vitamins & Supplements",
                    Code = "VS",
                    Description = "Dietary supplements and nutritional products",
                    Status = "Pending",
                    ColorClass = "bg-green-100 text-green-600",
                    ItemCount = 0,
                    TenantUsage = 0,
                    RequestedBy = "MedCare Pharmacy",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Personal Care",
                    Code = "PC",
                    Description = "Personal hygiene and care products",
                    Status = "Active",
                    ColorClass = "bg-pink-100 text-pink-600",
                    ItemCount = 156,
                    TenantUsage = 78,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "First Aid Supplies",
                    Code = "FA",
                    Description = "Emergency medical supplies and first aid equipment",
                    Status = "Active",
                    ColorClass = "bg-orange-100 text-orange-600",
                    ItemCount = 89,
                    TenantUsage = 92,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }
    }
}
