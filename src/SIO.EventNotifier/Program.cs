using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SIO.EntityFrameworkCore.DbContexts;
using SIO.EventNotifier;
using SIO.Infrastructure.EntityFrameworkCore.Extensions;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .Build();

var env = host.Services.GetRequiredService<IHostEnvironment>();

if (env.IsDevelopment())
{
    await host.RunProjectionMigrationsAsync();
    await host.RunStoreMigrationsAsync<SIOEventNotifierStoreDbContext>();
}

await host.RunAsync();
