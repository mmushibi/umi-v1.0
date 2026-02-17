using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace UmiHealthPOS.Hubs
{
    public class BillingHub : Hub
    {
        private readonly ILogger<BillingHub> _logger;

        public BillingHub(ILogger<BillingHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinBillingGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "BillingUpdates");
            _logger.LogInformation("Client joined billing updates group");
        }

        public async Task NotifyInvoiceUpdate(object invoiceUpdate)
        {
            await Clients.Group("BillingUpdates").SendAsync("InvoiceUpdated", invoiceUpdate);
            _logger.LogInformation($"Invoice update broadcasted: {invoiceUpdate}");
        }

        public async Task NotifyTenantUpdate(object tenantUpdate)
        {
            await Clients.Group("BillingUpdates").SendAsync("TenantUpdated", tenantUpdate);
            _logger.LogInformation($"Tenant update broadcasted: {tenantUpdate}");
        }
    }
}
