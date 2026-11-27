using Cubing.Core.Packing;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Services;
using Cubing.Core.Packing.Strategies;
using Microsoft.Extensions.Options;

namespace Cubing.Api.Extensions;

public static class CubingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the cubing services, binds <see cref="CubingOptions"/> from the <c>Cubing</c> section (validated at
    /// startup) and the customer profiles. Super Nice Cubes is built in; <paramref name="configure"/> adds more
    /// customers, each with its own packing strategy and LPN generator.
    /// </summary>
    public static IServiceCollection AddCubing(
        this IServiceCollection services, Action<CustomerProfileRegistry>? configure = null)
    {
        services.AddOptions<CubingOptions>()
            .BindConfiguration(CubingOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultCustomerId), "Cubing:DefaultCustomerId must not be blank.")
            .ValidateOnStart();
        // Core does not reference Microsoft.Extensions.Options, so it receives the plain options object.
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<CubingOptions>>().Value);

        var registry = new CustomerProfileRegistry()
            .Add(CubingOptions.SuperNiceCubes, new CubePackingStrategy(), new GuidLpnGenerator())
            .Add(CubingOptions.SuperSafelyPackedCubes, new CubePackingStrategy(), new DateTimeLpnGenerator());
        configure?.Invoke(registry);

        services.AddSingleton<ICustomerProfileResolver>(registry);
        services.AddScoped<ICubingService, CubingService>();
        services.AddScoped<ICubingCalculationService, CubingCalculationService>();
        return services;
    }
}
