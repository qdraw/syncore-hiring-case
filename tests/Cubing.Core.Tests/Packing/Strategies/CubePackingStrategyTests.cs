using Cubing.Core.Packing.Models;
using Cubing.Core.Packing.Strategies;

namespace Cubing.Core.Tests.Packing.Strategies;

public class CubePackingStrategyTests
{
    private readonly CubePackingStrategy _strategy = new();

    [Theory]
    [InlineData(10, 10, 10, 20, 20, 20, 8)]
    [InlineData(10, 10, 10, 35, 35, 35, 27)]
    [InlineData(10, 10, 10, 45, 45, 45, 64)]
    [InlineData(8, 8, 8, 16, 13, 44, 10)]
    [InlineData(10, 10, 10, 12, 12, 30, 3)]
    [InlineData(10, 10, 10, 10, 10, 10, 1)]
    public void Capacity_is_the_product_of_articles_per_axis(
        int al, int aw, int ah, int bl, int bw, int bh, long expected)
    {
        var capacity = _strategy.GetMaxArticlesPerBox(new Dimensions(al, aw, ah), new Dimensions(bl, bw, bh));

        Assert.Equal(expected, capacity);
    }

    [Theory]
    [InlineData(10, 5, 5, 5)]
    [InlineData(10, 100, 100, 9)] // one axis too small
    public void Capacity_is_zero_when_the_article_does_not_fit(int edge, int bl, int bw, int bh)
    {
        var capacity = _strategy.GetMaxArticlesPerBox(new Dimensions(edge, edge, edge), new Dimensions(bl, bw, bh));

        Assert.Equal(0, capacity);
    }

    [Fact]
    public void Capacity_is_zero_for_non_positive_dimensions()
    {
        Assert.Equal(0, _strategy.GetMaxArticlesPerBox(new Dimensions(0, 0, 0), new Dimensions(10, 10, 10)));
        Assert.Equal(0, _strategy.GetMaxArticlesPerBox(new Dimensions(1, 1, 1), new Dimensions(-1, 10, 10)));
    }

    [Fact]
    public void Capacity_saturates_instead_of_overflowing()
    {
        var capacity = _strategy.GetMaxArticlesPerBox(
            new Dimensions(1, 1, 1), new Dimensions(int.MaxValue, int.MaxValue, int.MaxValue));

        Assert.Equal(long.MaxValue, capacity);
    }
}
