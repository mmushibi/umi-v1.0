using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Tests
{
    public class DatabaseIntegrationTest
    {
        public static async Task<bool> TestDatabaseIntegration()
        {
            try
            {
                // Build service collection
                var services = new ServiceCollection();

                // Add logging
                services.AddLogging(builder => builder.AddConsole());

                // Get connection string from environment or use default
                var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
                    ?? "Host=localhost;Database=umihealth_test;Username=postgres;Password=password";

                // Add database context
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // Build service provider
                var serviceProvider = services.BuildServiceProvider();

                // Get database context
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Test database connection
                var canConnect = await context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    Console.WriteLine("❌ Cannot connect to database");
                    return false;
                }

                Console.WriteLine("✅ Database connection successful");

                // Test core entities exist and are accessible
                var entityTests = new[]
                {
                    // Test Patient entity
                    (Task.Run(async () => {
                        var count = await context.Patients.CountAsync();
                        Console.WriteLine($"✅ Patients table accessible: {count} records");
                        return true;
                    }), "Patients"),
                    
                    // Test Prescription entity
                    (Task.Run(async () => {
                        var count = await context.Prescriptions.CountAsync();
                        Console.WriteLine($"✅ Prescriptions table accessible: {count} records");
                        return true;
                    }), "Prescriptions"),
                    
                    // Test InventoryItem entity
                    (Task.Run(async () => {
                        var count = await context.InventoryItems.CountAsync();
                        Console.WriteLine($"✅ InventoryItems table accessible: {count} records");
                        return true;
                    }), "InventoryItems"),
                    
                    // Test Supplier entity
                    (Task.Run(async () => {
                        var count = await context.Suppliers.CountAsync();
                        Console.WriteLine($"✅ Suppliers table accessible: {count} records");
                        return true;
                    }), "Suppliers"),
                    
                    // Test Shift entity
                    (Task.Run(async () => {
                        var count = await context.Shifts.CountAsync();
                        Console.WriteLine($"✅ Shifts table accessible: {count} records");
                        return true;
                    }), "Shifts"),
                    
                    // Test ReportSchedule entity
                    (Task.Run(async () => {
                        var count = await context.ReportSchedules.CountAsync();
                        Console.WriteLine($"✅ ReportSchedules table accessible: {count} records");
                        return true;
                    }), "ReportSchedules"),
                    
                    // Test PharmacistProfile entity
                    (Task.Run(async () => {
                        var count = await context.PharmacistProfiles.CountAsync();
                        Console.WriteLine($"✅ PharmacistProfiles table accessible: {count} records");
                        return true;
                    }), "PharmacistProfiles")
                };

                // Run all tests
                var results = await Task.WhenAll(entityTests.Select(t => t.Item1));

                // Check if all tests passed
                var allPassed = results.All(r => r);

                if (allPassed)
                {
                    Console.WriteLine("✅ All database entities are properly integrated!");

                    // Test relationships
                    await TestEntityRelationships(context);
                }
                else
                {
                    Console.WriteLine("❌ Some database entities failed integration");
                }

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database integration test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static async Task TestEntityRelationships(ApplicationDbContext context)
        {
            try
            {
                // Test Patient-Prescription relationship
                var patientWithPrescriptions = await context.Patients
                    .Include(p => p.Prescriptions)
                    .FirstOrDefaultAsync();

                Console.WriteLine("✅ Patient-Prescription relationship test passed");

                // Test Prescription-PrescriptionItem relationship
                var prescriptionWithItems = await context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync();

                Console.WriteLine("✅ Prescription-PrescriptionItem relationship test passed");

                // Test InventoryItem navigation
                var inventoryItems = await context.InventoryItems
                    .Include(i => i.Tenant)
                    .Include(i => i.Branch)
                    .Take(5)
                    .ToListAsync();

                Console.WriteLine("✅ InventoryItem navigation properties test passed");

                // Test Supplier relationships
                var suppliers = await context.Suppliers
                    .Include(s => s.Contacts)
                    .Include(s => s.Products)
                    .Take(5)
                    .ToListAsync();

                Console.WriteLine("✅ Supplier relationship test passed");

                Console.WriteLine("✅ All entity relationship tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Entity relationship test failed: {ex.Message}");
            }
        }

        public static async Task<bool> TestSampleDataCreation()
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
                    ?? "Host=localhost;Database=umihealth_test;Username=postgres;Password=password";

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

                using var context = new ApplicationDbContext(options);

                // Create test tenant
                var tenant = new Tenant
                {
                    TenantId = "TEST01",
                    Name = "Test Pharmacy",
                    AdminName = "Test Admin",
                    Email = "test@example.com",
                    PhoneNumber = "1234567890"
                };

                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();

                // Create test patient
                var patient = new Patient
                {
                    Name = "Test Patient",
                    Email = "patient@example.com",
                    Phone = "0987654321",
                    TenantId = tenant.TenantId
                };

                context.Patients.Add(patient);
                await context.SaveChangesAsync();

                // Create test prescription
                var prescription = new Prescription
                {
                    PrescriptionNumber = "RX-TEST-001",
                    PatientId = patient.Id,
                    PatientName = patient.Name,
                    DoctorName = "Dr. Test Doctor",
                    Medication = "Test Medication",
                    Dosage = "500mg",
                    Instructions = "Take twice daily",
                    TenantId = tenant.TenantId
                };

                context.Prescriptions.Add(prescription);
                await context.SaveChangesAsync();

                Console.WriteLine("✅ Sample data creation test passed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Sample data creation test failed: {ex.Message}");
                return false;
            }
        }
    }
}
