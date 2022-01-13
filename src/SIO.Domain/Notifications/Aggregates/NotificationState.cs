using SIO.Infrastructure.Domain;
using System;

namespace SIO.Domain.Notifications.Aggregates
{
    public sealed class NotificationState : IAggregateState
    {
        public int Attempts { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTimeOffset? PublicationDate { get; set; }
        public string EventSubject { get; set; }

        public NotificationState() { }
        public NotificationState(NotificationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Attempts = state.Attempts;
            Status = state.Status;
            PublicationDate = state.PublicationDate;
            EventSubject = state.EventSubject;
        }
    }
}
