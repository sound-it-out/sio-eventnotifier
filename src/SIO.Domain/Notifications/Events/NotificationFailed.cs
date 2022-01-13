using SIO.Infrastructure.Events;

namespace SIO.Domain.Notifications.Events
{
    public class NotificationFailed : Event
    {
        public string EventSubject { get; }
        public string Error { get; }

        public NotificationFailed(string error, string subject, int version, string eventSubject) : base(subject, version)
        {
            EventSubject = eventSubject;
            Error = error;
        }
    }
}
