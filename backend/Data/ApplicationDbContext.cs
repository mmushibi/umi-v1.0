using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasPrecision(10, 2);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Barcode).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Barcode).IsUnique();
                entity.HasIndex(e => e.Category);
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Sale configuration
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Subtotal).HasPrecision(10, 2);
                entity.Property(e => e.Tax).HasPrecision(10, 2);
                entity.Property(e => e.Total).HasPrecision(10, 2);
                entity.Property(e => e.CashReceived).HasPrecision(10, 2);
                entity.Property(e => e.Change).HasPrecision(10, 2);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).HasMaxLength(20);
                
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Sales)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // SaleItem configuration
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Sale)
                      .WithMany(s => s.SaleItems)
                      .HasForeignKey(e => e.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.SaleItems)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // StockTransaction configuration
            modelBuilder.Entity<StockTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Reason).HasMaxLength(200);
                
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.StockTransactions)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.ProductId, e.CreatedAt });
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed initial products
            modelBuilder.Entity<Product>().HasData(
                new Product 
                { 
                    Id = 1, 
                    Name = "Paracetamol 500mg", 
                    Category = "Medications", 
                    Price = 5.99m, 
                    Stock = 50, 
                    Barcode = "1234567890", 
                    Description = "Pain relief medication",
                    MinStock = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "Ibuprofen 400mg", 
                    Category = "Medications", 
                    Price = 7.49m, 
                    Stock = 30, 
                    Barcode = "1234567891", 
                    Description = "Anti-inflammatory medication",
                    MinStock = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product 
                { 
                    Id = 3, 
                    Name = "Face Masks", 
                    Category = "Medical Supplies", 
                    Price = 12.99m, 
                    Stock = 100, 
                    Barcode = "1234567892", 
                    Description = "Disposable face masks",
                    MinStock = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product 
                { 
                    Id = 4, 
                    Name = "Hand Sanitizer", 
                    Category = "Personal Care", 
                    Price = 4.99m, 
                    Stock = 75, 
                    Barcode = "1234567893", 
                    Description = "Alcohol-based hand sanitizer",
                    MinStock = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product 
                { 
                    Id = 5, 
                    Name = "Vitamin C 1000mg", 
                    Category = "Vitamins", 
                    Price = 9.99m, 
                    Stock = 60, 
                    Barcode = "1234567894", 
                    Description = "Vitamin C supplement",
                    MinStock = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed initial customers
            modelBuilder.Entity<Customer>().HasData(
                new Customer 
                { 
                    Id = 1, 
                    Name = "Walk-in Customer", 
                    Email = "walkin@umihealth.com", 
                    Phone = "000-000-0000", 
                    Address = "Pharmacy Counter",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Customer 
                { 
                    Id = 2, 
                    Name = "John Doe", 
                    Email = "john.doe@example.com", 
                    Phone = "123-456-7890", 
                    Address = "123 Main St, Lusaka",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Customer 
                { 
                    Id = 3, 
                    Name = "Jane Smith", 
                    Email = "jane.smith@example.com", 
                    Phone = "098-765-4321", 
                    Address = "456 Oak Ave, Lusaka",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
