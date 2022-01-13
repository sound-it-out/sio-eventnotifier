using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIO.Domain.Notifications.Events;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.Projections;

namespace SIO.Domain.Notifications.Projections.Managers
{
    public sealed class NotificationQueueProjectionManager : ProjectionManager<NotificationQueue>
    {
        private readonly IEnumerable<IProjectionWriter<NotificationQueue>> _projectionWriters;
        private readonly ISIOProjectionDbContextFactory _projectionDbContextFactory;
        private readonly NotificationOptions _notificationOptions;

        public NotificationQueueProjectionManager(ILogger<ProjectionManager<NotificationQueue>> logger,
            IEnumerable<IProjectionWriter<NotificationQueue>> projectionWriters,
            ISIOProjectionDbContextFactory projectionDbContextFactory,
            IOptionsSnapshot<NotificationOptions> optionsSnapshot) : base(logger)
        {
            if (projectionWriters == null)
                throw new ArgumentNullException(nameof(projectionWriters));
            if (projectionDbContextFactory == null)
                throw new ArgumentNullException(nameof(projectionDbContextFactory));
            if (optionsSnapshot == null)
                throw new ArgumentNullException(nameof(optionsSnapshot));

            _projectionWriters = projectionWriters;
            _projectionDbContextFactory = projectionDbContextFactory;
            _notificationOptions = optionsSnapshot.Value;

            Handle<NotificationQueued>(HandleAsync);
            Handle<NotificationFailed>(HandleAsync);
            Handle<NotificationSucceded>(HandleAsync);
        }

        public async Task HandleAsync(NotificationQueued @event, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationQueueProjectionManager)}.{nameof(HandleAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            await Task.WhenAll(_projectionWriters.Select(pw => pw.AddAsync(@event.Subject, () => new NotificationQueue
            {
                Attempts = 0,
                Subject = @event.Subject,
                EventSubject = @event.EventSubject,
                PublicationDate = @event.PublicationDate
            }, cancellationToken)));
        }

        public async Task HandleAsync(NotificationFailed @event, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationQueueProjectionManager)}.{nameof(HandleAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            using var context = _projectionDbContextFactory.Create();
            var notification = await context.Set<NotificationQueue>().FindAsync(new object[] { @event.Subject }, cancellationToken: cancellationToken);
            if (notification.Attempts == _notificationOptions.MaxRetries)
            {
                await Task.WhenAll(_projectionWriters.Select(pw => pw.RemoveAsync(@event.Subject)));
            }
            else
            {
                await Task.WhenAll(_projectionWriters.Select(pw => pw.UpdateAsync(@event.Subject, epq =>
                {
                    epq.Attempts++;
                })));
            }
        }

        public async Task HandleAsync(NotificationSucceded @event, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationQueueProjectionManager)}.{nameof(HandleAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            await Task.WhenAll(_projectionWriters.Select(pw => pw.RemoveAsync(@event.Subject)));
        }

        public override async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationQueueProjectionManager)}.{nameof(ResetAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            await Task.WhenAll(_projectionWriters.Select(pw => pw.ResetAsync(cancellationToken)));
        }
    }
}
