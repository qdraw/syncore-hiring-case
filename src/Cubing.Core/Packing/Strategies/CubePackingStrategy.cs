using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Strategies;

/// <summary>
/// Packing for Super Nice Cubes: cubic articles stacked in a grid, so rotation is irrelevant and
/// capacity is the product of how many articles fit along each axis.
/// </summary>
public sealed class CubePackingStrategy : IPackingStrategy
{
    public string? ValidateArticle(Dimensions article) =>
        article.IsCubic ? null : "Article dimensions must be cubic (length = width = height).";

    public long GetMaxArticlesPerBox(Dimensions article, Dimensions box)
    {
        if (!article.IsPositive || !box.IsPositive || !article.IsCubic)
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
    // Both operands are non-negative here.
    private static long SaturatingMultiply(long a, long b) =>
        a == 0 || b <= long.MaxValue / a ? a * b : long.MaxValue;
}
