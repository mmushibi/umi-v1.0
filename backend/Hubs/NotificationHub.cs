using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using UmiHealthPOS.DTOs;

namespace UmiHealthPOS.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly string _userGroupPrefix = "user_";
        private readonly string _tenantGroupPrefix = "tenant_";

        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{_userGroupPrefix}{userId}");
        }

        public async Task JoinTenantGroup(string tenantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{_tenantGroupPrefix}{tenantId}");
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{_userGroupPrefix}{userId}");
        }

        public async Task LeaveTenantGroup(string tenantId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{_tenantGroupPrefix}{tenantId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst("userId")?.Value;
            var tenantId = Context.User?.FindFirst("tenantId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"{_userGroupPrefix}{userId}");
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"{_tenantGroupPrefix}{tenantId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst("userId")?.Value;
            var tenantId = Context.User?.FindFirst("tenantId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{_userGroupPrefix}{userId}");
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{_tenantGroupPrefix}{tenantId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
