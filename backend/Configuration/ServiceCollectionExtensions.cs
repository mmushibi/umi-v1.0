using Microsoft.Extensions.DependencyInjection;
using UmiHealthPOS.Services;
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

            // Register services
            // services.AddScoped<IDashboardService, DashboardService>();
            // services.AddScoped<IDashboardNotificationService, DashboardNotificationService>();
            // services.AddScoped<IInventoryService, InventoryService>();
            // services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IWebSearchService, WebSearchService>();
            services.AddScoped<ISepioAIService, SepioAIService>();
            services.AddScoped<ISessionTimeoutService, SessionTimeoutService>();

            // Add SignalR
            services.AddSignalR();

            // TODO: Add other services as needed
            // services.AddScoped<IUserService, UserService>();
            // services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}
