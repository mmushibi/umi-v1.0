using Microsoft.Extensions.DependencyInjection;
using UmiHealthPOS.Services;
using UmiHealthPOS.Hubs;

namespace UmiHealthPOS.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register services
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDashboardNotificationService, DashboardNotificationService>();
            services.AddScoped<ISuperAdminDashboardService, SuperAdminDashboardService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<ReportsService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPasswordService, PasswordService>();

            // Add SignalR
            services.AddSignalR();

            // TODO: Add other services as needed
            // services.AddScoped<IUserService, UserService>();
            // services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}
