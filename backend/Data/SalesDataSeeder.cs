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
    public class SalesDataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SalesDataSeeder> _logger;

        public SalesDataSeeder(ApplicationDbContext context, ILogger<SalesDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedSampleSalesData()
        {
            try
            {
                // Check if sales data already exists
                if (await _context.Sales.AnyAsync())
                {
                    _logger.LogInformation("Sales data already exists, skipping seeding");
                    return;
                }

                // Get existing products and customers
                var products = await _context.Products.ToListAsync();
                var customers = await _context.Customers.ToListAsync();

                if (!products.Any())
                {
                    _logger.LogWarning("No products found, skipping sales data seeding");
                    return;
                }

                // Create sample sales
                var random = new Random();
                var sales = new List<Sale>();

                for (int i = 1; i <= 20; i++)
                {
                    var saleDate = DateTime.UtcNow.AddDays(-random.Next(0, 30));
                    var customer = random.Next(0, 3) == 0 ? customers[random.Next(customers.Count)] : null;
                    
                    // Create sale items
                    var saleItems = new List<SaleItem>();
                    var itemCount = random.Next(1, 4);
                    var selectedProducts = products.OrderBy(p => random.Next()).Take(itemCount).ToList();

                    decimal subtotal = 0;
                    foreach (var product in selectedProducts)
                    {
                        var quantity = random.Next(1, 5);
                        var unitPrice = product.Price;
                        var totalPrice = unitPrice * quantity;
                        
                        saleItems.Add(new SaleItem
                        {
                            ProductId = product.Id,
                            UnitPrice = unitPrice,
                            Quantity = quantity,
                            TotalPrice = totalPrice
                        });
                        
                        subtotal += totalPrice;
                    }

                    // Calculate tax and total
                    var tax = subtotal * 0.16m; // 16% tax
                    var total = subtotal + tax;

                    var sale = new Sale
                    {
                        ReceiptNumber = $"RCP{DateTime.Now:yyyyMMdd}{i:D4}",
                        CustomerId = customer?.Id,
                        Subtotal = subtotal,
                        Tax = tax,
                        Total = total,
                        PaymentMethod = GetRandomPaymentMethod(random),
                        PaymentDetails = GetRandomPaymentDetails(random),
                        CashReceived = total + random.Next(0, 100),
                        Status = GetRandomStatus(random),
                        CreatedAt = saleDate,
                        UpdatedAt = saleDate,
                        SaleItems = saleItems
                    };

                    // Calculate change for cash payments
                    if (sale.PaymentMethod == "cash")
                    {
                        sale.Change = sale.CashReceived - total;
                    }

                    sales.Add(sale);
                }

                await _context.Sales.AddRangeAsync(sales);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully seeded {sales.Count} sample sales records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding sales data");
                throw;
            }
        }

        private string GetRandomPaymentMethod(Random random)
        {
            var methods = new[] { "cash", "card", "mobile", "insurance" };
            return methods[random.Next(methods.Length)];
        }

        private string GetRandomPaymentDetails(Random random)
        {
            var method = GetRandomPaymentMethod(random);
            return method switch
            {
                "card" => $"**** **** **** {random.Next(1000, 9999)}",
                "mobile" => $"+260{random.Next(900000000, 999999999)}",
                "insurance" => $"INS-{random.Next(10000, 99999)}",
                _ => ""
            };
        }

        private string GetRandomStatus(Random random)
        {
            var statuses = new[] { "completed", "completed", "completed", "pending", "refunded", "cancelled" };
            return statuses[random.Next(statuses.Length)];
        }
    }

    public static class SalesDataSeederExtensions
    {
        public static async Task<IHost> SeedSalesDataAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<SalesDataSeeder>>();
            
            try
            {
                var seeder = services.GetRequiredService<SalesDataSeeder>();
                await seeder.SeedSampleSalesData();
                logger.LogInformation("Sales data seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding sales data");
            }

            return host;
        }
    }
}
