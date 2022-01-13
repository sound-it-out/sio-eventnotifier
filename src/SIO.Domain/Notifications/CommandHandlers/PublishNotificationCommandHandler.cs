using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SIO.Domain.Notifications.Aggregates;
using SIO.Domain.Notifications.Commands;
using SIO.Domain.Notifications.Hubs;
using SIO.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure;
using SIO.Infrastructure.Commands;
using SIO.Infrastructure.Domain;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIO.Domain.Notifications.CommandHandlers
{
    internal sealed class PublishNotificationCommandHandler : ICommandHandler<PublishNotificationCommand>
    {
        private readonly ILogger<PublishNotificationCommandHandler> _logger;
        private readonly IAggregateRepository<SIOEventNotifierStoreDbContext> _aggregateRepository;
        private readonly IEventStore<SIOStoreDbContext> _eventStore;
        private readonly IHubContext<NotificationHub> _hubContext;

        public PublishNotificationCommandHandler(ILogger<PublishNotificationCommandHandler> logger,
            IAggregateRepository<SIOEventNotifierStoreDbContext> aggregateRepository,
            IEventStore<SIOStoreDbContext> eventStore,
            IHubContext<NotificationHub> hubContext)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (aggregateRepository == null)
                throw new ArgumentNullException(nameof(aggregateRepository));
            if (eventStore == null)
                throw new ArgumentNullException(nameof(eventStore));
            if (hubContext == null)
                throw new ArgumentNullException(nameof(hubContext));

            _logger = logger;
            _aggregateRepository = aggregateRepository;            
            _eventStore = eventStore;
            _hubContext = hubContext;
        }

        public async Task ExecuteAsync(PublishNotificationCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(PublishNotificationCommandHandler)}.{nameof(ExecuteAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            var aggregate = await _aggregateRepository.GetAsync<Notification, NotificationState>(command.Subject, cancellationToken);

            try
            {
                var state = aggregate.GetState();
                // Event has been published but we are ahead of projections!
                if (state.Status == NotificationStatus.Succeeded)
                    return;

                var eventContext = await _eventStore.GetEventAsync(Subject.From(state.EventSubject), cancellationToken);

                var notification = new EventNotification<IEvent>(streamId: eventContext.StreamId,
                    @event: eventContext.Payload,
                    correlationId: eventContext.CorrelationId,
                    causationId: eventContext.CausationId,
                    timestamp: eventContext.Payload.Timestamp,
                    userId: command.Actor);

                await _hubContext.NotifyAsync(notification, cancellationToken);

                aggregate.Succeed();
            }
            catch (Exception ex)
            {
                aggregate.Fail(ex.Message);
            }

            await _aggregateRepository.SaveAsync(aggregate, command, aggregate.Version - 1, cancellationToken);
        }
    }
}
