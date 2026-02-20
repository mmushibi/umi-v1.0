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
using UmiHealthPOS.Security;
using UmiHealthPOS.Models;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Developer: Sepio Corp
// Umi Health POS System - Backend Application

var builder = WebApplication.CreateBuilder(args);

// Configure hosts
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Configure allowed hosts based on environment
var allowedHosts = builder.Environment.IsProduction()
    ? "umihealth.zm,www.umihealth.zm,api.umihealth.zm"
    : "*";
builder.Configuration.AddInMemoryCollection([new("AllowedHosts", allowedHosts)]);

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured"),
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured"),
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
    };
});

builder.Services.AddAuthorization();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Database connection string not configured")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Environment.IsProduction()
            ? "https://umihealth.zm,https://www.umihealth.zm"
            : builder.Configuration["Frontend:AllowedOrigins"] ?? "http://localhost:3000";

        policy.WithOrigins(origins.Split(','))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add application services
builder.Services.AddApplicationServices();

// Add security services
builder.Services.AddScoped<IRowLevelSecurityService, RowLevelSecurityService>();
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Add subscription services
builder.Services.AddScoped<ISubscriptionExpirationService, SubscriptionExpirationService>();
builder.Services.AddHostedService<SubscriptionExpirationService>();
builder.Services.AddScoped<IUsageTrackingService, UsageTrackingService>();
builder.Services.AddScoped<ILimitService, LimitService>();
builder.Services.AddScoped<ISubscriptionNotificationService, SubscriptionNotificationService>();

// DataSeeder removed due to static type conflicts
// builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<SubscriptionDataSeeder>();

var app = builder.Build();

// app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<RowLevelSecurityMiddleware>();
app.UseSubscriptionMiddleware();
// app.UseBranchIsolation();
// app.UseInactivityCheck();
app.UseAuthorization();

// Map SignalR hubs
// app.MapHub<DashboardHub>("/dashboardHub");
// app.MapHub<PatientHub>("/patientHub");

app.MapControllers();

// Seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    {
        // Data seeding is handled by DataSeeder static class
        // await DataSeeder.SeedDataAsync(scope.ServiceProvider);
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


