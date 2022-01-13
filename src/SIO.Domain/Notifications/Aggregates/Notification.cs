using SIO.Domain.Notifications.Events;
using SIO.Infrastructure.Domain;
using System;

namespace SIO.Domain.Notifications.Aggregates
{
    public sealed class Notification : Aggregate<NotificationState>
    {
        public Notification(NotificationState state) : base(state)
        {
            Handles<NotificationQueued>(Handle);
            Handles<NotificationFailed>(Handle);
            Handles<NotificationSucceded>(Handle);
        }

        public override NotificationState GetState() => new NotificationState(_state);

        public void Queue(string subject,
            DateTimeOffset? publicationDate,
            string eventSubject)
        {
            Apply(new NotificationQueued(
                subject: subject,
                version: Version + 1,
                publicationDate: publicationDate,
                eventSubject: eventSubject
            ));
        }

        public void Fail(string error)
        {
            Apply(new NotificationFailed(
                error: error,
                subject: Id,
                version: Version + 1,
                eventSubject: _state.EventSubject
            ));
        }

        public void Succeed()
        {
            Apply(new NotificationSucceded(
                subject: Id,
                version: Version + 1
            ));
        }

        private void Handle(NotificationQueued @event)
        {
            Id = @event.Subject;
            _state.PublicationDate = @event.PublicationDate;
            _state.Attempts = 0;
            _state.Status = NotificationStatus.Queued;
            _state.EventSubject = @event.EventSubject;
            Version = @event.Version;
        }

        private void Handle(NotificationFailed @event)
        {
            _state.Attempts++;
            _state.Status = NotificationStatus.Failed;
            Version = @event.Version;
        }

        private void Handle(NotificationSucceded @event)
        {
            _state.Attempts++;
            _state.Status = NotificationStatus.Succeeded;
            Version = @event.Version;
        }
    }
}
