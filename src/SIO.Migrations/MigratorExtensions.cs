using Microsoft.EntityFrameworkCore;
using SIO.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;
using SIO.Infrastructure.EntityFrameworkCore.Migrations;

namespace SIO.Migrations
{
    public static class MigratorExtensions
    {
        public static Migrator AddContexts(this Migrator migrator)
            => migrator.WithDbContext<SIOProjectionDbContext>(o => o.UseSqlServer("Server=.,1452;Initial Catalog=sio-eventnotifier-projections;User Id=sa;Password=1qaz-pl,", b => b.MigrationsAssembly("SIO.Migrations")))
                .WithDbContext<SIOEventNotifierStoreDbContext>(o => o.UseSqlServer("Server=.,1452;Initial Catalog=sio-event-eventnotifier-store;User Id=sa;Password=1qaz-pl,", b => b.MigrationsAssembly("SIO.Migrations")));
    }
}
