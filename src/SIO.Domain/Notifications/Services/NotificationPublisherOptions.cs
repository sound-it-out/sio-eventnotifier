namespace SIO.Domain.Notifications.Services
{
    public class NotificationPublisherOptions
    {
        public int Interval { get; set; }
        public int MaxRetries { get; set; }
    }
}
