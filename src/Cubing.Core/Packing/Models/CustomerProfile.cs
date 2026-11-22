using Cubing.Core.Packing.Interfaces;

namespace Cubing.Core.Packing.Models;

/// <summary>The customer-specific behaviour used to cube a request: how articles pack and how LPNs look.</summary>
public sealed class CustomerProfile(IPackingStrategy packingStrategy, ILpnGenerator lpnGenerator)
{
    public IPackingStrategy PackingStrategy { get; } = packingStrategy;
    public ILpnGenerator LpnGenerator { get; } = lpnGenerator;
}
