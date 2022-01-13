using Microsoft.Extensions.DependencyInjection;
using SIO.Domain.Notifications;
using SIO.Domain.Notifications.CommandHandlers;
using SIO.Domain.Notifications.Commands;
using SIO.Domain.Notifications.Services;
using SIO.Infrastructure.Commands;

namespace SIO.Domain.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection services)
        {
            services.AddScoped<ICommandHandler<QueueNotificationCommand>, QueueNotificationCommandHandler>();
            services.AddScoped<ICommandHandler<PublishNotificationCommand>, PublishNotificationCommandHandler>();
            services.AddHostedService<EventProcessor>();
            services.AddHostedService<NotificationPublisher>();
            services.Configure<EventProcessorOptions>(o => o.Interval = 300);
            services.Configure<NotificationPublisherOptions>(o => o.Interval = 300);
            services.Configure<NotificationOptions>(o => o.MaxRetries = 5);
            return services;
        }
    }
}
