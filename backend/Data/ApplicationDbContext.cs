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
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

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

            // InventoryItem configuration
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InventoryItemName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.GenericName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.BrandName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.BatchNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LicenseNumber).HasMaxLength(100);
                entity.Property(e => e.ZambiaRegNumber).HasMaxLength(100);
                entity.Property(e => e.PackingType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
                entity.Property(e => e.SellingPrice).HasPrecision(10, 2);
                entity.Property(e => e.ManufactureDate).HasColumnType("date");
                
                entity.HasIndex(e => e.BatchNumber).IsUnique();
                entity.HasIndex(e => e.ZambiaRegNumber);
                entity.HasIndex(e => e.InventoryItemName);
            });

            // Prescription configuration
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RxNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PatientName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PatientIdNumber).HasMaxLength(20);
                entity.Property(e => e.DoctorName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DoctorRegistrationNumber).HasMaxLength(100);
                entity.Property(e => e.Medication).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Dosage).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Instructions).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TotalCost).HasPrecision(10, 2);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PrescriptionDate).HasColumnType("date");
                entity.Property(e => e.ExpiryDate).HasColumnType("date");
                entity.Property(e => e.FilledDate).HasColumnType("date");
                entity.Property(e => e.Notes).HasMaxLength(500);
                
                entity.HasIndex(e => e.RxNumber).IsUnique();
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PrescriptionDate);
                
                entity.HasOne(e => e.Patient)
                      .WithMany(p => p.Prescriptions)
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Patient configuration
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IdNumber).HasMaxLength(20);
                entity.Property(e => e.PhoneNumber).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.DateOfBirth).HasColumnType("date");
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.Allergies).HasMaxLength(100);
                entity.Property(e => e.MedicalHistory).HasMaxLength(500);
                
                entity.HasIndex(e => e.IdNumber).IsUnique();
                entity.HasIndex(e => e.Name);
            });

            // PrescriptionItem configuration
            modelBuilder.Entity<PrescriptionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MedicationName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Dosage).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Instructions).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Prescription)
                      .WithMany(p => p.PrescriptionItems)
                      .HasForeignKey(e => e.PrescriptionId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.InventoryItem)
                      .WithMany()
                      .HasForeignKey(e => e.InventoryItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
