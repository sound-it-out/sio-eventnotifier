using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIO.Infrastructure.EntityFrameworkCore.EntityConfiguration;

namespace SIO.Domain.Notifications.Projections.Configurations
{
    internal sealed class NotificationFailureTypeConfiguration : IProjectionTypeConfiguration<NotificationFailure>
    {
        public void Configure(EntityTypeBuilder<NotificationFailure> builder)
        {
            builder.ToTable(nameof(NotificationFailure));
            builder.HasKey(epf => epf.Subject);
            builder.Property(epf => epf.Subject)
                   .ValueGeneratedNever();
            builder.HasIndex(epf => epf.EventSubject);
        }
    }
}
