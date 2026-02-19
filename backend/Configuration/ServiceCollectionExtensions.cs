using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UmiHealthPOS.Services;
using UmiHealthPOS.Services.AI;
using UmiHealthPOS.BackgroundServices;
using UmiHealthPOS.Hubs;
using UmiHealthPOS.Repositories;

namespace UmiHealthPOS.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Add HttpContextAccessor for AuthService
            services.AddHttpContextAccessor();

            // Add HttpClient for WebSearchService and SepioAIService
            services.AddHttpClient();

            // Add MemoryCache for WebSearchService and SepioAIService
            services.AddMemoryCache();

            // Register repositories
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ISaleRepository, SaleRepository>();
            services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();

            // Register services
            // Note: Some services are currently excluded from compilation in .csproj
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            // services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDashboardNotificationService, DashboardNotificationService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IWebSearchService, WebSearchService>();
            services.AddScoped<ISepioAIService, SepioAIService>();
            services.AddScoped<AIDataService>();
            services.AddScoped<ISessionTimeoutService, SessionTimeoutService>();
            services.AddScoped<ISessionManagementService, SessionManagementService>();
            
            // Register notification and usage tracking services
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISubscriptionNotificationService, SubscriptionNotificationService>();
            services.AddScoped<IUsageTrackingService, UsageTrackingService>();
            services.AddScoped<ILimitService, LimitService>();
            services.AddScoped<ISubscriptionExpirationService, SubscriptionExpirationService>();

            // Register settings and system services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<IRealSettingsService, RealSettingsService>();

            // Add background services
            services.AddHostedService<SessionCleanupService>();

            // Add SignalR
            services.AddSignalR();
            
            // TODO: Add other services as needed
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IImpersonationService, ImpersonationService>();
            services.AddScoped<IZambianComplianceService, ZambianComplianceService>();

            return services;
        }
    }
}
