using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIO.Infrastructure.EntityFrameworkCore.EntityConfiguration;

namespace SIO.Domain.Notifications.Projections.Configurations
{
    internal sealed class NotificationQueueTypeConfiguration : IProjectionTypeConfiguration<NotificationQueue>
    {
        public void Configure(EntityTypeBuilder<NotificationQueue> builder)
        {
            builder.ToTable(nameof(NotificationQueue));
            builder.HasKey(epq => epq.Subject);
            builder.Property(epq => epq.Subject)
                   .ValueGeneratedNever();
            builder.HasIndex(epf => epf.EventSubject);
        }
    }
}
