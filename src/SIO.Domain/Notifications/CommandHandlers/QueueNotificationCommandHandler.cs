using Microsoft.Extensions.Logging;
using SIO.Domain.Notifications.Aggregates;
using SIO.Domain.Notifications.Commands;
using SIO.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.Commands;
using SIO.Infrastructure.Domain;
using SIO.Infrastructure.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIO.Domain.Notifications.CommandHandlers
{
    internal sealed class QueueNotificationCommandHandler : ICommandHandler<QueueNotificationCommand>
    {
        private readonly ILogger<QueueNotificationCommandHandler> _logger;
        private readonly IAggregateRepository<SIOEventNotifierStoreDbContext> _aggregateRepository;
        private readonly IAggregateFactory _aggregateFactory;

        public QueueNotificationCommandHandler(ILogger<QueueNotificationCommandHandler> logger,
            IAggregateRepository<SIOEventNotifierStoreDbContext> aggregateRepository,
            IAggregateFactory aggregateFactory)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (aggregateRepository == null)
                throw new ArgumentNullException(nameof(aggregateRepository));
            if (aggregateFactory == null)
                throw new ArgumentNullException(nameof(aggregateFactory));

            _logger = logger;
            _aggregateRepository = aggregateRepository;
            _aggregateFactory = aggregateFactory;
        }

        public async Task ExecuteAsync(QueueNotificationCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(QueueNotificationCommandHandler)}.{nameof(ExecuteAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            var aggregate = await _aggregateRepository.GetAsync<Notification, NotificationState>(command.Subject, cancellationToken);

            if (aggregate != null)
                return;

            aggregate = _aggregateFactory.FromHistory<Notification, NotificationState>(Enumerable.Empty<IEvent>());

            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));

            aggregate.Queue(
                subject: command.Subject,
                publicationDate: command.PublicationDate,
                eventSubject: command.EventSubject
            );

            await _aggregateRepository.SaveAsync(aggregate, command, cancellationToken: cancellationToken);
        }
    }
}
