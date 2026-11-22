using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;
using Cubing.Core.Packing.Services;
using Cubing.Core.Packing.Strategies;
using NSubstitute;

namespace Cubing.Core.Tests.Packing.Services;

public class CubingServiceTests
{
    private static CubingRequest Request(int quantity, int article, Dimensions box) =>
        new(quantity, "Article1", new Dimensions(article, article, article), "Box", box);

    private static CubingService RealService(ILpnGenerator? lpn = null) =>
        new(new CubePackingStrategy(), lpn ?? new GuidLpnGenerator());

    [Theory]
    [InlineData(20, 10, 20, 20, 20, new[] { 8, 8, 4 })]
    [InlineData(28, 10, 35, 35, 35, new[] { 27, 1 })]
    [InlineData(100, 10, 45, 45, 45, new[] { 64, 36 })]
    [InlineData(10, 8, 16, 13, 44, new[] { 10 })]
    [InlineData(10, 10, 12, 12, 12, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 })]
    [InlineData(4, 10, 20, 20, 20, new[] { 4 })] // quantity below capacity
    [InlineData(16, 10, 20, 20, 20, new[] { 8, 8 })] // exactly divisible
    public void Distributes_articles_over_boxes(
        int quantity, int article, int bl, int bw, int bh, int[] expectedItems)
    {
        var result = RealService().Cube(Request(quantity, article, new Dimensions(bl, bw, bh)));

        Assert.Equal(expectedItems, result.Boxes.Select(b => b.NumberOfItems));
        Assert.Equal(expectedItems.Length, result.TotalBoxes);
        Assert.Equal(quantity, result.TotalQuantity);
        Assert.Equal("Article1", result.ArticleName);
        Assert.Equal("Box", result.BoxTypeName);
    }

    [Fact]
    public void Returns_unique_lpn_per_box()
    {
        var result = RealService().Cube(Request(100, 10, new Dimensions(12, 12, 12)));

        Assert.Equal(100, result.Boxes.Select(b => b.LicensePlateNumber).Distinct().Count());
    }

    [Fact]
    public void Generates_one_lpn_per_box()
    {
        var lpn = Substitute.For<ILpnGenerator>();
        lpn.Generate().Returns("A", "B", "C");

        var result = RealService(lpn).Cube(Request(20, 10, new Dimensions(20, 20, 20)));

        lpn.Received(3).Generate();
        Assert.Equal(["A", "B", "C"], result.Boxes.Select(b => b.LicensePlateNumber));
    }

    [Fact]
    public void Retries_when_generator_returns_a_duplicate()
    {
        var lpn = Substitute.For<ILpnGenerator>();
        lpn.Generate().Returns("A", "A", "B");

        var result = RealService(lpn).Cube(Request(16, 10, new Dimensions(20, 20, 20)));

        Assert.Equal(["A", "B"], result.Boxes.Select(b => b.LicensePlateNumber));
        lpn.Received(3).Generate();
    }

    [Fact]
    public void Throws_when_generator_cannot_produce_a_unique_lpn()
    {
        var lpn = Substitute.For<ILpnGenerator>();
        lpn.Generate().Returns("SAME");

        Assert.Throws<InvalidOperationException>(
            () => RealService(lpn).Cube(Request(16, 10, new Dimensions(20, 20, 20))));
    }

    [Fact]
    public void Uses_the_configured_packing_strategy()
    {
        var strategy = Substitute.For<IPackingStrategy>();
        strategy.GetMaxArticlesPerBox(Arg.Any<Dimensions>(), Arg.Any<Dimensions>()).Returns(5L);
        var lpn = Substitute.For<ILpnGenerator>();
        lpn.Generate().Returns("1", "2", "3");
        var service = new CubingService(strategy, lpn);

        var result = service.Cube(Request(12, 10, new Dimensions(1, 1, 1)));

        Assert.Equal([5, 5, 2], result.Boxes.Select(b => b.NumberOfItems));
    }

    [Fact]
    public void Does_not_generate_lpns_for_invalid_requests()
    {
        var lpn = Substitute.For<ILpnGenerator>();

        Assert.Throws<CubingValidationException>(
            () => RealService(lpn).Cube(Request(10, 10, new Dimensions(5, 5, 5))));

        lpn.DidNotReceive().Generate();
    }

    [Fact]
    public void Rejects_article_that_does_not_fit_in_box()
    {
        var ex = Assert.Throws<CubingValidationException>(
            () => RealService().Cube(Request(10, 10, new Dimensions(5, 5, 5))));

        Assert.Contains(nameof(CubingRequest.ArticleDimensions), ex.Errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_non_positive_quantity(int quantity)
    {
        var ex = Assert.Throws<CubingValidationException>(
            () => RealService().Cube(Request(quantity, 10, new Dimensions(20, 20, 20))));

        Assert.Contains(nameof(CubingRequest.Quantity), ex.Errors.Keys);
    }

    [Theory]
    [InlineData(0, 20, 20)]
    [InlineData(20, -1, 20)]
    [InlineData(20, 20, 0)]
    public void Rejects_non_positive_box_dimensions(int l, int w, int h)
    {
        var ex = Assert.Throws<CubingValidationException>(
            () => RealService().Cube(Request(10, 10, new Dimensions(l, w, h))));

        Assert.Contains(nameof(CubingRequest.BoxDimensions), ex.Errors.Keys);
    }

    [Fact]
    public void Rejects_non_positive_article_dimensions()
    {
        var request = new CubingRequest(10, "A", new Dimensions(0, 0, 0), "Box", new Dimensions(20, 20, 20));

        var ex = Assert.Throws<CubingValidationException>(() => RealService().Cube(request));

        Assert.Contains(nameof(CubingRequest.ArticleDimensions), ex.Errors.Keys);
    }

    [Fact]
    public void Rejects_non_cubic_article()
    {
        var request = new CubingRequest(10, "A", new Dimensions(10, 10, 11), "Box", new Dimensions(40, 40, 40));

        var ex = Assert.Throws<CubingValidationException>(() => RealService().Cube(request));

        Assert.Contains("cubic", ex.Errors[nameof(CubingRequest.ArticleDimensions)][0]);
    }

    [Theory]
    [InlineData("", "Box", nameof(CubingRequest.ArticleName))]
    [InlineData("A", " ", nameof(CubingRequest.BoxTypeName))]
    public void Rejects_missing_names(string article, string box, string expectedKey)
    {
        var request = new CubingRequest(10, article, new Dimensions(10, 10, 10), box, new Dimensions(20, 20, 20));

        var ex = Assert.Throws<CubingValidationException>(() => RealService().Cube(request));

        Assert.Contains(expectedKey, ex.Errors.Keys);
    }

    [Fact]
    public void Rejects_requests_that_need_too_many_boxes()
    {
        var ex = Assert.Throws<CubingValidationException>(
            () => RealService().Cube(Request(int.MaxValue, 1, new Dimensions(1, 1, 1))));

        Assert.Contains(nameof(CubingRequest.Quantity), ex.Errors.Keys);
    }

    [Fact]
    public void Handles_huge_capacity_without_overflow()
    {
        var result = RealService().Cube(
            Request(int.MaxValue, 1, new Dimensions(int.MaxValue, int.MaxValue, int.MaxValue)));

        Assert.Equal(1, result.TotalBoxes);
        Assert.Equal(int.MaxValue, result.Boxes[0].NumberOfItems);
    }
}
