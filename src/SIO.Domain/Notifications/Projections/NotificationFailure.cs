using SIO.Infrastructure.Projections;

namespace SIO.Domain.Notifications.Projections
{
    public class NotificationFailure : IProjection
    {
        public string Subject { get; set; }
        public string EventSubject { get; set; }
        public string Error { get; set; }
    }
}
