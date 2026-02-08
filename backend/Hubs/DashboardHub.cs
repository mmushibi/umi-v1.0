using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace UmiHealthPOS.Hubs
{
    public class DashboardHub : Hub
    {
        private readonly ILogger<DashboardHub> _logger;

        public DashboardHub(ILogger<DashboardHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinTenantGroup(string tenantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            _logger.LogInformation($"User {Context.ConnectionId} joined tenant group: {tenantId}");
        }

        public async Task LeaveTenantGroup(string tenantId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            _logger.LogInformation($"User {Context.ConnectionId} left tenant group: {tenantId}");
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"User connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"User disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
