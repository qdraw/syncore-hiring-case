using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Services;

/// <summary>In-memory customer id to profile mapping. Customer ids are case-insensitive.</summary>
public sealed class CustomerProfileRegistry : ICustomerProfileResolver
{
    private readonly Dictionary<string, CustomerProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public CustomerProfileRegistry Add(string customerId, IPackingStrategy packingStrategy, ILpnGenerator lpnGenerator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentNullException.ThrowIfNull(packingStrategy);
        ArgumentNullException.ThrowIfNull(lpnGenerator);

        return !_profiles.TryAdd(customerId, new CustomerProfile(packingStrategy, lpnGenerator)) ? throw new ArgumentException($"Customer '{customerId}' is already registered.", nameof(customerId)) : this;
    }

    public CustomerProfile? Resolve(string customerId) =>
        string.IsNullOrWhiteSpace(customerId) ? null : _profiles.GetValueOrDefault(customerId);
}
