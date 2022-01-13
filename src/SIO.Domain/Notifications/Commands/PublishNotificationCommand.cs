using SIO.Infrastructure;
using SIO.Infrastructure.Commands;

namespace SIO.Domain.Notifications.Commands
{
    public class PublishNotificationCommand : Command
    {
        public PublishNotificationCommand(string subject, CorrelationId? correlationId, int version, Actor actor) : base(subject, correlationId, version, actor)
        {
        }
    }
}
