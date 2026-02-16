using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace UmiHealthPOS.Hubs
{
    [Authorize]
    public class PatientHub : Hub
    {
        public async Task JoinPatientGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Patients");
        }

        public async Task LeavePatientGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Patients");
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", "Connected to Patient Hub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
