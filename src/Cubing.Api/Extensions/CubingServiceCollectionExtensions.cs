using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Services;
using Cubing.Core.Packing.Strategies;

namespace Cubing.Api.Extensions;

public static class CubingServiceCollectionExtensions
{
    public const string SuperNiceCubes = "SuperNiceCubes";

    /// <summary>
    /// Registers the customer profiles. Super Nice Cubes is built in; <paramref name="configure"/> adds more
    /// customers, each with its own packing strategy and LPN generator.
    /// </summary>
    public static IServiceCollection AddCubing(
        this IServiceCollection services, Action<CustomerProfileRegistry>? configure = null)
    {
        var registry = new CustomerProfileRegistry()
            .Add(SuperNiceCubes, new CubePackingStrategy(), new GuidLpnGenerator());
        configure?.Invoke(registry);

        services.AddSingleton<ICustomerProfileResolver>(registry);
        services.AddScoped<ICubingService, CubingService>();
        return services;
    }
}
