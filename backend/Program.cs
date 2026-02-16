using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UmiHealthPOS.Configuration;
using UmiHealthPOS.Hubs;
using UmiHealthPOS.Data;
using UmiHealthPOS.Repositories;
using UmiHealthPOS.Services;
using UmiHealthPOS.Middleware;
using Npgsql;

// Developer: Sepio Corp
// Umi Health POS System - Backend Application

var builder = WebApplication.CreateBuilder(args);

// Configure hosts
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Configure allowed hosts
builder.Configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
{
    new KeyValuePair<string, string?>("AllowedHosts", "*")
});

// Add services to the container.
builder.Services.AddControllers();

// Add Authentication and Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Bearer";
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:AllowedOrigins"]?.Split(',') ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add application services
builder.Services.AddApplicationServices();

// Add repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();

// Add business services
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ISubscriptionHistoryService, SubscriptionHistoryService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IPatientService, PatientService>();

// DataSeeder removed due to static type conflicts
// builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<SubscriptionDataSeeder>();
builder.Services.AddScoped<CategoryDataSeeder>();

var app = builder.Build();

// app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
// app.UseAuthentication();
// app.UseBranchIsolation();
// app.UseInactivityCheck();
// app.UseAuthorization();

// Map SignalR hubs
app.MapHub<DashboardHub>("/dashboardHub");
app.MapHub<PatientHub>("/patientHub");

app.MapControllers();

// Seed data in development
if (app.Environment.IsDevelopment())
{
    await DataSeeder.SeedDataAsync(app.Services);
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await CategoryDataSeeder.SeedCategoriesAsync(context);

        // Seed pharmacist dashboard sample data
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var seederLogger = loggerFactory.CreateLogger<PharmacistDashboardSeeder>();
        var pharmacistSeeder = new PharmacistDashboardSeeder(context, seederLogger);
        await pharmacistSeeder.SeedSampleDataAsync();
    }
}

app.Run();

// Make Program accessible for testing
public partial class Program { }

// Extension method for inactivity middleware
public static class InactivityMiddlewareExtensions
{
    public static IApplicationBuilder UseInactivityCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<InactivityMiddleware>();
    }
}


