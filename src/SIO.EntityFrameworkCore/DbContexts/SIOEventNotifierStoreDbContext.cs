using Microsoft.EntityFrameworkCore;
using SIO.Infrastructure.EntityFrameworkCore.DbContexts;

namespace SIO.EntityFrameworkCore.DbContexts
{
    public class SIOEventNotifierStoreDbContext : SIOStoreDbContextBase<SIOEventNotifierStoreDbContext>
    {
        public SIOEventNotifierStoreDbContext(DbContextOptions<SIOEventNotifierStoreDbContext> options) : base(options)
        {
        }
    }
}
