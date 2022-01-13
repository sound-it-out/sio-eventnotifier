using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIO.Domain.Notifications.Commands;
using SIO.Domain.Notifications.Projections;
using SIO.Infrastructure;
using SIO.Infrastructure.Commands;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;

namespace SIO.Domain.Notifications.Services
{
    internal sealed class NotificationPublisher : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource StoppingCts { get; set; }
        private readonly IServiceScope _scope;
        private readonly ILogger<NotificationPublisher> _logger;
        private readonly IOptionsSnapshot<NotificationPublisherOptions> _options;
        private readonly ISIOProjectionDbContextFactory _projectionDbContextFactory;
        private readonly string _name;
        private readonly ICommandDispatcher _commandDispatcher;

        public NotificationPublisher(IServiceScopeFactory serviceScopeFactory,
            ILogger<NotificationPublisher> logger)
        {
            if (serviceScopeFactory == null)
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _logger = logger;
            _options = _scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<NotificationPublisherOptions>>();
            _projectionDbContextFactory = _scope.ServiceProvider.GetRequiredService<ISIOProjectionDbContextFactory>();
            _commandDispatcher = _scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            _name = typeof(NotificationPublisher).FullName;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationPublisher)}.{nameof(StartAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger.LogInformation($"{nameof(NotificationPublisher)} starting");
            StoppingCts = new();

            _executingTask = ExecuteAsync(StoppingCts.Token);

            _logger.LogInformation($"{nameof(NotificationPublisher)} started");

            if (_executingTask.IsCompleted)
                return _executingTask;

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationPublisher)}.{nameof(StopAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger.LogInformation($"{nameof(NotificationPublisher)} stopping");

            if (_executingTask == null)
                return;

            try
            {
                StoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
                _logger.LogInformation($"{nameof(NotificationPublisher)} stopped");
            }
        }

        private async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{nameof(NotificationPublisher)}.{nameof(ExecuteAsync)} was cancelled before execution");
                cancellationToken.ThrowIfCancellationRequested();
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var context = _projectionDbContextFactory.Create();
                    var eventsInQueue = await context.Set<NotificationQueue>()
                        .AsQueryable()
                        .Where(epq => !epq.PublicationDate.HasValue || epq.PublicationDate <= DateTimeOffset.UtcNow)
                        .Take(50)
                        .Select(epq => epq.Subject)
                        .ToArrayAsync(cancellationToken);

                    var correlationId = CorrelationId.New();

                    foreach (var @event in eventsInQueue)
                    {
                        await _commandDispatcher.DispatchAsync(new PublishNotificationCommand(
                            subject: @event,
                            correlationId: correlationId,
                            version: 0,
                            Actor.Unknown
                        ), cancellationToken);
                    }

                    if (eventsInQueue.Length == 0)
                        await Task.Delay(_options.Value.Interval, cancellationToken);
                    else
                        await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, $"Process '{typeof(NotificationPublisher).Name}' failed due to an unexpected error. See exception details for more information.");
                    break;
                }
            }
        }
    }
}
