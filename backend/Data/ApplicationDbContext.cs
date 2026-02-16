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

        public required DbSet<Product> Products { get; set; } = null!;
        public required DbSet<Customer> Customers { get; set; } = null!;
        public required DbSet<Sale> Sales { get; set; } = null!;
        public required DbSet<SaleItem> SaleItems { get; set; } = null!;
        public required DbSet<StockTransaction> StockTransactions { get; set; } = null!;
        public required DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public required DbSet<Prescription> Prescriptions { get; set; } = null!;
        public required DbSet<Patient> Patients { get; set; } = null!;
        public required DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public required DbSet<ReportSchedule> ReportSchedules { get; set; } = null!;
        public required DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public required DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public required DbSet<Subscription> Subscriptions { get; set; } = null!;
        public required DbSet<SubscriptionHistory> SubscriptionHistories { get; set; } = null!;
        public required DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public required DbSet<UserSession> UserSessions { get; set; } = null!;
        public required DbSet<UserAccount> Users { get; set; } = null!;
        public required DbSet<Branch> Branches { get; set; } = null!;
        public required DbSet<UserBranch> UserBranches { get; set; } = null!;
        public required DbSet<Tenant> Tenants { get; set; } = null!;
        public required DbSet<UserAccount> UserAccounts { get; set; } = null!;
        public required DbSet<DaybookTransaction> DaybookTransactions { get; set; } = null!;
        public required DbSet<DaybookTransactionItem> DaybookTransactionItems { get; set; } = null!;
        public required DbSet<Invoice> Invoices { get; set; } = null!;
        public required DbSet<CreditNote> CreditNotes { get; set; } = null!;
        public required DbSet<Payment> Payments { get; set; } = null!;
        public required DbSet<ControlledSubstance> ControlledSubstances { get; set; } = null!;
        public required DbSet<Shift> Shifts { get; set; } = null!;
        public required DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;
        public required DbSet<Employee> Employees { get; set; } = null!;
        public required DbSet<ControlledSubstanceAudit> ControlledSubstanceAudits { get; set; } = null!;

        // RBAC Entities
        public required DbSet<Role> Roles { get; set; } = null!;
        public required DbSet<Permission> Permissions { get; set; } = null!;
        public required DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public required DbSet<UserRole> UserRoles { get; set; } = null!;
        public required DbSet<TenantRole> TenantRoles { get; set; } = null!;

        // System Entities
        public required DbSet<Notification> Notifications { get; set; } = null!;
        public required DbSet<SystemSetting> SystemSettings { get; set; } = null!;
        public required DbSet<SettingsAuditLog> SettingsAuditLogs { get; set; } = null!;
        public required DbSet<AppSetting> AppSettings { get; set; } = null!;

        // Application Feature Management
        public required DbSet<ApplicationFeature> ApplicationFeatures { get; set; } = null!;
        public required DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; } = null!;

        // Category Management
        public required DbSet<CategorySync> CategorySyncs { get; set; } = null!;
        public required DbSet<TenantCategory> TenantCategories { get; set; } = null!;

        // Supplier Management
        public required DbSet<Supplier> Suppliers { get; set; } = null!;
        public required DbSet<SupplierContact> SupplierContacts { get; set; } = null!;
        public required DbSet<SupplierProduct> SupplierProducts { get; set; } = null!;

        // Pharmacist Account Management
        public required DbSet<PharmacistProfile> PharmacistProfiles { get; set; }

        // Clinical Management
        public required DbSet<ClinicalNote> ClinicalNotes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
                entity.Property(e => e.SellingPrice).HasPrecision(10, 2);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
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
                entity.Property(e => e.ReceiptNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Subtotal).HasPrecision(10, 2);
                entity.Property(e => e.Tax).HasPrecision(10, 2);
                entity.Property(e => e.Total).HasPrecision(10, 2);
                entity.Property(e => e.CashReceived).HasPrecision(10, 2);
                entity.Property(e => e.Change).HasPrecision(10, 2);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PaymentDetails).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RefundReason).HasMaxLength(500);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Sales)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ReceiptNumber).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Status);
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

                entity.HasOne(e => e.Branch)
                      .WithMany(b => b.InventoryItems)
                      .HasForeignKey(e => e.BranchId)
                      .OnDelete(DeleteBehavior.SetNull);

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
                      .WithMany()
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

            // ReportSchedule configuration
            modelBuilder.Entity<ReportSchedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Frequency).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DateRange).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Format).IsRequired().HasMaxLength(10);
                entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(450);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Branch)
                      .WithMany()
                      .HasForeignKey(e => e.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ReportType);
                entity.HasIndex(e => e.NextRunAt);
                entity.HasIndex(e => e.CreatedBy);
            });

            // UserAccount configuration
            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("Cashier");
                entity.Property(e => e.TenantId).IsRequired().HasMaxLength(6).HasDefaultValue("UMI001");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Branch)
                      .WithMany(b => b.Users)
                      .HasForeignKey(e => e.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Role);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.BranchId);
            });

            // Pharmacy configuration
            modelBuilder.Entity<Pharmacy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LicenseNumber).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Province).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.Country).HasMaxLength(50).HasDefaultValue("Zambia");
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
            });

            // SubscriptionPlan configuration
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasPrecision(10, 2);
                entity.Property(e => e.Features).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
            });

            // Subscription configuration
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(10, 2);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Plan)
                      .WithMany(p => p.Subscriptions)
                      .HasForeignKey(e => e.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Pharmacy)
                      .WithMany()
                      .HasForeignKey(e => e.PharmacyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PharmacyId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
            });

            // ActivityLog configuration
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("success");
                entity.Property(e => e.IpAddress).HasMaxLength(100);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });

            // UserSession configuration
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DeviceInfo).HasMaxLength(100);
                entity.Property(e => e.Browser).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Token);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsActive);
            });

            // Branch configuration
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Region).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(50).HasDefaultValue("Zambia");
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ManagerName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ManagerPhone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.OperatingHours).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.MonthlyRevenue).HasDefaultValue(0).HasPrecision(10, 2);
                entity.Property(e => e.StaffCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Region);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsActive);
            });

            // UserBranch configuration
            modelBuilder.Entity<UserBranch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.BranchId).IsRequired();
                entity.Property(e => e.UserRole).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Permission).IsRequired().HasMaxLength(20).HasDefaultValue("read");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Branch)
                      .WithMany(b => b.UserBranches)
                      .HasForeignKey(e => e.BranchId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserBranches)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.UserRole);
                entity.HasIndex(e => e.IsActive);
            });

            // Supplier configuration
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SupplierCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BusinessName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TradeName).HasMaxLength(200);
                entity.Property(e => e.RegistrationNumber).HasMaxLength(100);
                entity.Property(e => e.TaxIdentificationNumber).HasMaxLength(50);
                entity.Property(e => e.PharmacyLicenseNumber).HasMaxLength(100);
                entity.Property(e => e.DrugSupplierLicense).HasMaxLength(100);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.ContactPersonTitle).HasMaxLength(50);
                entity.Property(e => e.PrimaryPhoneNumber).HasMaxLength(20);
                entity.Property(e => e.SecondaryPhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.AlternativeEmail).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(200);
                entity.Property(e => e.PhysicalAddress).HasMaxLength(500);
                entity.Property(e => e.PostalAddress).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Province).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100).HasDefaultValue("Zambia");
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.BusinessType).HasMaxLength(50);
                entity.Property(e => e.Industry).HasMaxLength(100);
                entity.Property(e => e.AnnualRevenue).HasPrecision(15, 2);
                entity.Property(e => e.BankName).HasMaxLength(100);
                entity.Property(e => e.BankAccountNumber).HasMaxLength(50);
                entity.Property(e => e.BankAccountName).HasMaxLength(100);
                entity.Property(e => e.BankBranch).HasMaxLength(100);
                entity.Property(e => e.BankCode).HasMaxLength(20);
                entity.Property(e => e.SwiftCode).HasMaxLength(20);
                entity.Property(e => e.PaymentTerms).HasMaxLength(50).HasDefaultValue("Net 30");
                entity.Property(e => e.CreditLimit).HasDefaultValue(0.00m).HasPrecision(15, 2);
                entity.Property(e => e.CreditPeriod).HasDefaultValue(30);
                entity.Property(e => e.DiscountTerms).HasMaxLength(100);
                entity.Property(e => e.EarlyPaymentDiscount).HasDefaultValue(0.00m).HasPrecision(5, 2);
                entity.Property(e => e.SupplierCategory).HasMaxLength(50);
                entity.Property(e => e.SupplierStatus).HasMaxLength(20).HasDefaultValue("Active");
                entity.Property(e => e.PriorityLevel).HasMaxLength(20).HasDefaultValue("Medium");
                entity.Property(e => e.BlacklistReason).HasMaxLength(500);
                entity.Property(e => e.OnTimeDeliveryRate).HasDefaultValue(0.00m).HasPrecision(5, 2);
                entity.Property(e => e.QualityRating).HasDefaultValue(0.00m).HasPrecision(3, 2);
                entity.Property(e => e.PriceCompetitiveness).HasDefaultValue(0.00m).HasPrecision(3, 2);
                entity.Property(e => e.OverallRating).HasDefaultValue(0.00m).HasPrecision(3, 2);
                entity.Property(e => e.RegulatoryComplianceStatus).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.TenantId).IsRequired().HasMaxLength(6);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Tenant)
                      .WithMany(t => t.Suppliers)
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.SupplierCode).IsUnique();
                entity.HasIndex(e => e.BusinessName);
                entity.HasIndex(e => e.RegistrationNumber);
                entity.HasIndex(e => e.BusinessType);
                entity.HasIndex(e => e.SupplierCategory);
                entity.HasIndex(e => e.SupplierStatus);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsPreferred);
                entity.HasIndex(e => e.OverallRating);
            });

            // SupplierContact configuration
            modelBuilder.Entity<SupplierContact>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContactName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ContactTitle).HasMaxLength(50);
                entity.Property(e => e.Department).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.MobileNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.Contacts)
                      .HasForeignKey(e => e.SupplierId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.SupplierId);
                entity.HasIndex(e => e.IsPrimary);
                entity.HasIndex(e => e.IsOrderContact);
            });

            // SupplierProduct configuration
            modelBuilder.Entity<SupplierProduct>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SupplierProductCode).HasMaxLength(100);
                entity.Property(e => e.SupplierProductName).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.UnitCost).HasDefaultValue(0.00m).HasPrecision(10, 2);
                entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("ZMW");
                entity.Property(e => e.MinimumOrderQuantity).HasDefaultValue(1);
                entity.Property(e => e.OrderMultiples).HasDefaultValue(1);
                entity.Property(e => e.MinimumOrderValue).HasDefaultValue(0.00m).HasPrecision(10, 2);
                entity.Property(e => e.QualityGrade).HasMaxLength(20);
                entity.Property(e => e.BatchNumber).HasMaxLength(100);
                entity.Property(e => e.StorageRequirements).HasMaxLength(200);
                entity.Property(e => e.SupplierCatalogNumber).HasMaxLength(100);
                entity.Property(e => e.SupplierBarcode).HasMaxLength(100);
                entity.Property(e => e.PackagingInformation).HasMaxLength(200);
                entity.Property(e => e.WeightPerUnit).HasPrecision(10, 3);
                entity.Property(e => e.Dimensions).HasMaxLength(50);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.Products)
                      .HasForeignKey(e => e.SupplierId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.InventoryItem)
                      .WithMany()
                      .HasForeignKey(e => e.InventoryItemId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.SupplierId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.InventoryItemId);
                entity.HasIndex(e => e.IsAvailable);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => new[] { e.SupplierId, e.ProductId }).IsUnique();
                entity.HasIndex(e => new[] { e.SupplierId, e.InventoryItemId }).IsUnique();
            });

            // PharmacistProfile configuration
            modelBuilder.Entity<PharmacistProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.TenantId).IsRequired().HasMaxLength(6);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.LicenseNumber).HasMaxLength(100);
                entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("en");
                entity.Property(e => e.TwoFactorSecret).HasMaxLength(500);
                entity.Property(e => e.ProfilePicture).HasMaxLength(500);
                entity.Property(e => e.Signature).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.User)
                      .WithOne()
                      .HasForeignKey<PharmacistProfile>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Tenant)
                      .WithMany()
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.LicenseNumber);
                entity.HasIndex(e => e.IsActive);
            });

            // SubscriptionHistory configuration
            modelBuilder.Entity<SubscriptionHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PreviousPlan).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NewPlan).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).HasPrecision(10, 2);
                entity.Property(e => e.Notes).IsRequired().HasMaxLength(500);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Subscription)
                      .WithMany()
                      .HasForeignKey(e => e.SubscriptionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.SubscriptionId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ClinicalNote configuration
            modelBuilder.Entity<ClinicalNote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NoteType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Diagnosis).HasMaxLength(500);
                entity.Property(e => e.Symptoms).HasMaxLength(1000);
                entity.Property(e => e.Treatment).HasMaxLength(1000);
                entity.Property(e => e.TenantId).IsRequired().HasMaxLength(6);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(450);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Tenant)
                      .WithMany()
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Patient)
                      .WithMany()
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.CreatedBy);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.NoteType);
            });
        }
    }
}
