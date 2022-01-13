using SIO.Domain.Notifications.Events;
using System;
using System.Linq;

namespace SIO.Domain
{
    public static class EventHelper
    {
        public static Type[] AllEvents = new IntegrationEvents.AllEvents().Concat(new Type[]
        {
            typeof(NotificationQueued),
            typeof(NotificationFailed),
            typeof(NotificationSucceded)
        }).ToArray();
    }
}
