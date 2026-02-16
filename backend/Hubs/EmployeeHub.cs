using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace UmiHealthPOS.Hubs
{
    [Authorize]
    public class EmployeeHub : Hub
    {
        public async Task NotifyEmployeeCreated(int tenantId, object employee)
        {
            await Clients.Group(tenantId.ToString()).SendAsync("EmployeeCreated", employee);
        }

        public async Task NotifyEmployeeUpdated(int tenantId, object employee)
        {
            await Clients.Group(tenantId.ToString()).SendAsync("EmployeeUpdated", employee);
        }

        public async Task NotifyEmployeeDeleted(int tenantId, int employeeId)
        {
            await Clients.Group(tenantId.ToString()).SendAsync("EmployeeDeleted", employeeId);
        }

        public async Task NotifyPasswordReset(int tenantId, int employeeId, string newPassword)
        {
            await Clients.Group(tenantId.ToString()).SendAsync("PasswordReset", new { employeeId, newPassword });
        }

        public async Task JoinTenantGroup(int tenantId)
        {
            await Groups.AddToGroupAsync(tenantId.ToString());
        }
    }
}
