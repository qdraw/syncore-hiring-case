using Cubing.Core.Repositories;
using Cubing.Infrastructure.Data;
using Cubing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cubing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CubingDbContext>(o =>
            o.UseSqlite(configuration.GetConnectionString("Cubing") ?? "Data Source=cubing.db"));
        services.AddScoped<ICubingCalculationRepository, CubingCalculationRepository>();
        return services;
    }
}
