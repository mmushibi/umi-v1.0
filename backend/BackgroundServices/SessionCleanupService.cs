using Microsoft.Extensions.Hosting;
using UmiHealthPOS.Services;
using Microsoft.Extensions.Logging;

namespace UmiHealthPOS.BackgroundServices
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupService> _logger;

        public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessions(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during session cleanup.");
                }

                // Run cleanup every 15 minutes
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }

            _logger.LogInformation("Session Cleanup Service is stopping.");
        }

        private async Task CleanupExpiredSessions(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var sessionManagementService = scope.ServiceProvider.GetRequiredService<ISessionManagementService>();

            await sessionManagementService.CleanupExpiredSessionsAsync();
            _logger.LogDebug("Session cleanup completed at {Time}", DateTime.UtcNow);
        }
    }
}
