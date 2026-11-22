using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Services;
using Cubing.Core.Packing.Strategies;

namespace Cubing.Api.Extensions;

public static class CubingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Super Nice Cubes strategies. A future customer-specific setup would replace the
    /// strategy registrations here, or introduce a resolver in front of <see cref="ICubingService"/>.
    /// </summary>
    public static IServiceCollection AddCubing(this IServiceCollection services)
    {
        services.AddSingleton<IPackingStrategy, CubePackingStrategy>();
        services.AddSingleton<ILpnGenerator, GuidLpnGenerator>();
        services.AddScoped<ICubingService, CubingService>();
        return services;
    }
}
