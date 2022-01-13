using Microsoft.AspNetCore.SignalR;
using SIO.Infrastructure.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SIO.Domain.Notifications.Hubs
{
    internal static class NotificationHubExtensions
    {
        private static readonly Lazy<MethodInfo> _notifyAsyncMethod = new(() => typeof(NotificationHubExtensions)
            .GetMethod(nameof(NotificationHubExtensions.InternalNotifyAsync), BindingFlags.Static | BindingFlags.NonPublic));

        private static readonly Lazy<MethodInfo> _makeInstanceMethod = new(() => typeof(NotificationHubExtensions)
            .GetMethod(nameof(NotificationHubExtensions.MakeInstance), BindingFlags.Static | BindingFlags.NonPublic));

        private static readonly ConcurrentDictionary<Type, MethodInfo> _cachedMethods = new();
        private static readonly ConcurrentDictionary<Type, Type> _cachedActivationTypes = new();
        private static readonly ConcurrentDictionary<Type, MethodInfo> _cachedActivationMethods = new();



        public static async Task NotifyAsync(this IHubContext<NotificationHub> source, IEventNotification<IEvent> @event, CancellationToken cancellationToken = default)
        {
            var type = @event.Payload.GetType();

            if (!_cachedActivationMethods.TryGetValue(type, out var activator))
            {
                activator = _makeInstanceMethod.Value.MakeGenericMethod(type);
                _cachedActivationMethods.TryAdd(type, activator);
            }            

            if (!_cachedMethods.TryGetValue(type, out var method))
            {
                method = _notifyAsyncMethod.Value.MakeGenericMethod(type);
                _cachedMethods.TryAdd(type, method);
            }

            var notificationInstance = activator.Invoke(null, new object[] { @event });

            await (Task)method.Invoke(null, new object[] { source, notificationInstance, cancellationToken });                        
        }

        private static IEventNotification<TEvent> MakeInstance<TEvent>(IEventNotification<IEvent> eventNotification) where TEvent : IEvent
        {
            var type = typeof(TEvent);

            if (!_cachedActivationTypes.TryGetValue(type, out var activationType))
            {
                activationType = typeof(EventNotification<>).MakeGenericType(type);
                _cachedActivationTypes.TryAdd(type, activationType);
            }

            return (IEventNotification<TEvent>)Activator.CreateInstance(activationType, new object[] {
                eventNotification.StreamId,
                eventNotification.Payload,
                eventNotification.CorrelationId,
                eventNotification.CausationId,
                eventNotification.Timestamp,
                eventNotification.UserId
            });
        }

        private static Task InternalNotifyAsync<TEvent>(this IHubContext<NotificationHub> source, IEventNotification<TEvent> @event, CancellationToken cancellationToken = default) where TEvent : IEvent
                => source.Clients.All.SendAsync(typeof(TEvent).Name, @event, cancellationToken);
    }
}
