using Cubing.Core.Packing.Interfaces;

namespace Cubing.Core.Packing.Strategies;

public sealed class GuidLpnGenerator : ILpnGenerator
{
    public string Generate() => Guid.NewGuid().ToString();
}
