using Microsoft.AspNetCore.SignalR;

namespace SIO.EventNotifier.SignalR
{
    public class UserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection) => connection?.User?.FindFirst("sub").Value;
    }
}
