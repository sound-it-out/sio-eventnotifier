using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIO.Domain.Notifications.Commands;
using SIO.Infrastructure;
using SIO.Infrastructure.Commands;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.EntityFrameworkCore.Entities;
using SIO.Infrastructure.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIO.Domain.Notifications.Services
{
    internal sealed class EventProcessor : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource StoppingCts { get; set; }
        private readonly IServiceScope _scope;
        private readonly IEventStore<SIOStoreDbContext> _eventStore;
        private readonly ILogger<EventProcessor> _logger;
        private readonly IOptionsMonitor<EventProcessorOptions> _options;
        private readonly ISIOProjectionDbContextFactory _projectionDbContextFactory;
        private readonly string _name;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly HashSet<string> _eventsToProcess;

        public EventProcessor(IServiceScopeFactory serviceScopeFactory,
            IOptionsMonitor<EventProcessorOptions> options,
            ILogger<EventProcessor> logger)
        {
            if (serviceScopeFactory == null)
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _logger = logger;
            _eventStore = _scope.ServiceProvider.GetRequiredService<IEventStore<SIOStoreDbContext>>();
            _options = options;
            _projectionDbContextFactory = _scope.ServiceProvider.GetRequiredService<ISIOProjectionDbContextFactory>();
            _commandDispatcher = _scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            _name = typeof(EventProcessor).FullName;
            _eventsToProcess = new HashSet<string>(new IntegrationEvents.AllEvents().Select(t => t.FullName));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(EventProcessor)}.{nameof(StartAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger.LogInformation($"{nameof(EventProcessor)} starting");
            StoppingCts = new();

            _executingTask = ExecuteAsync(StoppingCts.Token);

            _logger.LogInformation($"{nameof(EventProcessor)} started");

            if (_executingTask.IsCompleted)
                return _executingTask;

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(EventProcessor)}.{nameof(StopAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger.LogInformation($"{nameof(EventProcessor)} stopping");

            if (_executingTask == null)
                return;

            try
            {
                StoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
                _logger.LogInformation($"{nameof(EventProcessor)} stopped");
            }
        }

        private async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(EventProcessor)}.{nameof(ExecuteAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            using var context = _projectionDbContextFactory.Create();
            var state = await context.ProjectionStates.FindAsync(new object[] { _name }, cancellationToken: cancellationToken);

            if (state == null)
            {
                state = new ProjectionState
                {
                    Name = _name,
                    CreatedDate = DateTimeOffset.UtcNow,
                    Position = 1
                };

                context.ProjectionStates.Add(state);

                await context.SaveChangesAsync(cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await context.Entry(state).ReloadAsync(cancellationToken);

                    var page = await _eventStore.GetEventsAsync(state.Position, cancellationToken);
                    var correlationId = CorrelationId.New();
                    foreach (var @event in page.Events.Where(e => _eventsToProcess.Contains(e.Payload.GetType().FullName)))
                    {
                        await _commandDispatcher.DispatchAsync(new QueueNotificationCommand(
                            subject: Subject.New(),
                            correlationId: correlationId,
                            version: 0,
                            Actor.Unknown,
                            @event.ScheduledPublication,
                            eventSubject: @event.Payload.Id
                        ), cancellationToken);
                    }

                    if (state.Position == page.Offset)
                    {
                        await Task.Delay(_options.CurrentValue.Interval, cancellationToken);
                    }
                    else
                    {
                        state.Position = page.Offset;
                        state.LastModifiedDate = DateTimeOffset.UtcNow;

                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, $"Process '{typeof(EventProcessor).Name}' failed at postion '{state.Position}' due to an unexpected error. See exception details for more information.");
                    break;
                }
            }
        }
    }
}
