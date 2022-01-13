using SIO.Infrastructure;
using SIO.Infrastructure.Commands;
using System;

namespace SIO.Domain.Notifications.Commands
{
    public class QueueNotificationCommand : Command
    {
        public DateTimeOffset? PublicationDate { get; }
        public string EventSubject {  get; }
        public QueueNotificationCommand(string subject,
            CorrelationId? correlationId,
            int version,
            Actor actor,
            DateTimeOffset? publicationDate,
            string eventSubject) : base(subject, correlationId, version, actor)
        {
            PublicationDate = publicationDate;
            EventSubject = eventSubject;
        }
    }
}
