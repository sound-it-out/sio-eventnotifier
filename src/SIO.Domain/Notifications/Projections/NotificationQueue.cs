using SIO.Infrastructure.Projections;
using System;

namespace SIO.Domain.Notifications.Projections
{
    public class NotificationQueue : IProjection
    {
        public string Subject { get; set; }
        public string EventSubject { get; set; }
        public int Attempts { get; set; }
        public DateTimeOffset? PublicationDate { get; set; }
    }
}
