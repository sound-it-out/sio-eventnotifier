using SIO.Infrastructure.Events;
using System;

namespace SIO.Domain.Notifications.Events
{
    public class NotificationQueued : Event
    {
        public DateTimeOffset? PublicationDate { get; }
        public string EventSubject { get; set; }

        public NotificationQueued(string subject, int version, DateTimeOffset? publicationDate, string eventSubject) : base(subject, version)
        {
            PublicationDate = publicationDate;
            EventSubject = eventSubject;
        }
    }
}
