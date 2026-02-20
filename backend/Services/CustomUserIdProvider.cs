using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace UmiHealthPOS.Services
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Try to get the user ID from JWT claims
            var userId = connection.User?.FindFirst("sub")?.Value
                        ?? connection.User?.FindFirst("userId")?.Value
                        ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return userId ?? connection.ConnectionId;
        }
    }
}
