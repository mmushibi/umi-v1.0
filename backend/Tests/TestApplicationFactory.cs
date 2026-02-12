using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Data;
using UmiHealthPOS.Repositories;
using Moq;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Tests;

public class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Create mock repositories
            var mockProductRepository = new Mock<IProductRepository>();
            var mockCustomerRepository = new Mock<ICustomerRepository>();
            var mockSaleRepository = new Mock<ISaleRepository>();
            var mockStockTransactionRepository = new Mock<IStockTransactionRepository>();

            // Replace repository registrations with mocks
            services.AddSingleton(mockProductRepository.Object);
            services.AddSingleton(mockCustomerRepository.Object);
            services.AddSingleton(mockSaleRepository.Object);
            services.AddSingleton(mockStockTransactionRepository.Object);

            // Create the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureCreated();
        });
    }
}
