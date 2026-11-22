using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Services;
using Cubing.Core.Packing.Strategies;
using Cubing.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cubing.Api.Tests;

public class CubingApiFactory : WebApplicationFactory<Program>
{
    public const string PrefixedCustomer = "PrefixedCubes";

    private sealed class PrefixedLpnGenerator : ILpnGenerator
    {
        private int _next;
        public string Generate() => $"PFX-{Interlocked.Increment(ref _next):D6}";
    }

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<CubingDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<CubingDbContext>(o => o.UseSqlite(_connection));

            // A second customer with its own LPN format, to prove selection works end to end.
            services.Remove(services.Single(d => d.ServiceType == typeof(ICustomerProfileResolver)));
            services.AddSingleton<ICustomerProfileResolver>(new CustomerProfileRegistry()
                .Add("SuperNiceCubes", new CubePackingStrategy(), new GuidLpnGenerator())
                .Add(PrefixedCustomer, new CubePackingStrategy(), new PrefixedLpnGenerator()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
