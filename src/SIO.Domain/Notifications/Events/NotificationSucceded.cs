using SIO.Infrastructure.Events;

namespace SIO.Domain.Notifications.Events
{
    public class NotificationSucceded : Event
    {
        public NotificationSucceded(string subject, int version) : base(subject, version)
        {
        }
    }
}
