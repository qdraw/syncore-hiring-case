using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Strategies;

/// <summary>
/// Packing for Super Nice Cubes: cubic articles stacked in a grid, so rotation is irrelevant and
/// capacity is the product of how many articles fit along each axis.
/// </summary>
public sealed class CubePackingStrategy : IPackingStrategy
{
    public long GetMaxArticlesPerBox(Dimensions article, Dimensions box)
    {
        if (!article.IsPositive || !box.IsPositive)
        {
            return 0;
        }

        // Articles are cubic, so any one edge describes them.
        long edge = article.Length;
        var alongLength = box.Length / edge;
        var alongWidth = box.Width / edge;
        var alongHeight = box.Height / edge;

        return SaturatingMultiply(SaturatingMultiply(alongLength, alongWidth), alongHeight);
    }

    // Each factor is below 2^31, so the product of three can exceed long. Clamp instead of wrapping.
    private static long SaturatingMultiply(long a, long b)
    {
        try
        {
            // checked(a * b) evaluates a * b with integer overflow checking enabled.
            return checked(a * b);
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }
}
