using SIO.Domain.Notifications.Projections;
using SIO.Domain.Notifications.Projections.Managers;
using SIO.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.EntityFrameworkCore.Extensions;

namespace SIO.Domain.Extensions
{
    public static class EntityFrameworkCoreStoreProjectorOptionsExtensions
    {
        public static void WithDomainProjections(this EntityFrameworkCoreStoreProjectorOptions options)
            => options.WithProjection<NotificationFailure, NotificationFailureProjectionManager, SIOEventNotifierStoreDbContext>(o => o.Interval = 5000)
                .WithProjection<NotificationQueue, NotificationQueueProjectionManager, SIOEventNotifierStoreDbContext>(o => o.Interval = 5000);
    }
}
