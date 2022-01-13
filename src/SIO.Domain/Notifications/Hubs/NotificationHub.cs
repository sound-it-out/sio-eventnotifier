using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SIO.Domain.Notifications.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public sealed class NotificationHub : Hub
    {
    }
}
