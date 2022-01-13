using Microsoft.Extensions.Logging;
using SIO.Domain.Notifications.Events;
using SIO.Infrastructure;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIO.Domain.Notifications.Projections.Managers
{
    public sealed class NotificationFailureProjectionManager : ProjectionManager<NotificationFailure>
    {
        private readonly IEnumerable<IProjectionWriter<NotificationFailure>> _projectionWriters;
        private readonly ISIOProjectionDbContextFactory _projectionDbContextFactory;

        public NotificationFailureProjectionManager(ILogger<ProjectionManager<NotificationFailure>> logger,
            IEnumerable<IProjectionWriter<NotificationFailure>> projectionWriters) : base(logger)
        {
            if (projectionWriters == null)
                throw new ArgumentNullException(nameof(projectionWriters));

            _projectionWriters = projectionWriters;

            Handle<NotificationFailed>(HandleAsync);
        }

        public async Task HandleAsync(NotificationFailed @event, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationQueueProjectionManager)}.{nameof(HandleAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            await Task.WhenAll(_projectionWriters.Select(pw => pw.AddAsync(@event.Subject, () => new NotificationFailure
            {
                Subject = @event.Subject,
                Error = @event.Error,
                EventSubject = @event.EventSubject
            }, cancellationToken)));
        }

        public override async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationFailureProjectionManager)}.{nameof(ResetAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            await Task.WhenAll(_projectionWriters.Select(pw => pw.ResetAsync(cancellationToken)));
        }
    }
}
