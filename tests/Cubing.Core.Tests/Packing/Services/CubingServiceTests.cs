using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;
using Cubing.Core.Packing.Services;
using Cubing.Core.Packing.Strategies;
using NSubstitute;

namespace Cubing.Core.Tests.Packing.Services;

public class CubingServiceTests
{
    private const string Customer = "SuperNiceCubes";

    private static CubingRequest Request(int quantity, int article, Dimensions box, string customer = Customer) =>
        new(customer, quantity, "Article1", new Dimensions(article, article, article), "Box", box);

    /// <summary>A strategy substitute that accepts every article (NSubstitute would return "" for the error message).</summary>
    private static IPackingStrategy AcceptingStrategy(long capacity)
    {
        var strategy = Substitute.For<IPackingStrategy>();
        strategy.ValidateArticle(Arg.Any<Dimensions>()).Returns((string?)null);
        strategy.GetMaxArticlesPerBox(Arg.Any<Dimensions>(), Arg.Any<Dimensions>()).Returns(capacity);
        return strategy;
    }

    private static CubingService ServiceFor(IPackingStrategy strategy, ILpnGenerator lpn) =>
        new(new CustomerProfileRegistry().Add(Customer, strategy, lpn));

    private static CubingService RealService(ILpnGenerator? lpn = null) =>
        ServiceFor(new CubePackingStrategy(), lpn ?? new GuidLpnGenerator());

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
        var strategy = AcceptingStrategy(5);
        var lpn = Substitute.For<ILpnGenerator>();
        lpn.Generate().Returns("1", "2", "3");
        var service = ServiceFor(strategy, lpn);

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
        var request = new CubingRequest(Customer, 10, "A", new Dimensions(0, 0, 0), "Box", new Dimensions(20, 20, 20));

        var ex = Assert.Throws<CubingValidationException>(() => RealService().Cube(request));

        Assert.Contains(nameof(CubingRequest.ArticleDimensions), ex.Errors.Keys);
    }

    [Fact]
    public void Rejects_non_cubic_article()
    {
        var request = new CubingRequest(Customer, 10, "A", new Dimensions(10, 10, 11), "Box", new Dimensions(40, 40, 40));

        var ex = Assert.Throws<CubingValidationException>(() => RealService().Cube(request));

        Assert.Contains("cubic", ex.Errors[nameof(CubingRequest.ArticleDimensions)][0]);
    }

    [Theory]
    [InlineData("", "Box", nameof(CubingRequest.ArticleName))]
    [InlineData("A", " ", nameof(CubingRequest.BoxTypeName))]
    public void Rejects_missing_names(string article, string box, string expectedKey)
    {
        var request = new CubingRequest(Customer, 10, article, new Dimensions(10, 10, 10), box, new Dimensions(20, 20, 20));

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

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_unknown_customer_without_generating_lpns(string customer)
    {
        var lpn = Substitute.For<ILpnGenerator>();

        var ex = Assert.Throws<CubingValidationException>(
            () => RealService(lpn).Cube(Request(10, 10, new Dimensions(20, 20, 20), customer)));

        Assert.Contains(nameof(CubingRequest.CustomerId), ex.Errors.Keys);
        lpn.DidNotReceive().Generate();
    }

    [Fact]
    public void Customer_id_is_matched_case_insensitively()
    {
        var result = RealService().Cube(Request(10, 10, new Dimensions(20, 20, 20), "supernicecubes"));

        Assert.Equal(2, result.TotalBoxes);
    }

    [Fact]
    public void Each_customer_uses_its_own_strategy_and_lpn_generator()
    {
        var strategyA = AcceptingStrategy(2);
        var lpnA = Substitute.For<ILpnGenerator>();
        lpnA.Generate().Returns("A1", "A2");
        var strategyB = AcceptingStrategy(10);
        var lpnB = Substitute.For<ILpnGenerator>();
        lpnB.Generate().Returns("B1");
        var service = new CubingService(new CustomerProfileRegistry()
            .Add("A", strategyA, lpnA)
            .Add("B", strategyB, lpnB));
        var box = new Dimensions(20, 20, 20);

        var a = service.Cube(Request(4, 10, box, "A"));
        var b = service.Cube(Request(4, 10, box, "B"));

        Assert.Equal(["A1", "A2"], a.Boxes.Select(x => x.LicensePlateNumber));
        Assert.Equal(["B1"], b.Boxes.Select(x => x.LicensePlateNumber));
        Assert.Equal("A", a.CustomerId);
        lpnA.Received(2).Generate();
        lpnB.Received(1).Generate();
    }

    [Fact]
    public void Article_rules_come_from_the_customers_strategy()
    {
        var strategy = AcceptingStrategy(3);
        var request = new CubingRequest(Customer, 3, "A", new Dimensions(10, 10, 11), "Box", new Dimensions(40, 40, 40));

        var result = ServiceFor(strategy, new GuidLpnGenerator()).Cube(request);

        Assert.Equal(1, result.TotalBoxes);
    }

    [Fact]
    public void Strategy_article_error_is_reported_as_a_validation_error()
    {
        var strategy = Substitute.For<IPackingStrategy>();
        strategy.ValidateArticle(Arg.Any<Dimensions>()).Returns("nope");

        var ex = Assert.Throws<CubingValidationException>(
            () => ServiceFor(strategy, new GuidLpnGenerator()).Cube(Request(1, 10, new Dimensions(20, 20, 20))));

        Assert.Equal(["nope"], ex.Errors[nameof(CubingRequest.ArticleDimensions)]);
        strategy.DidNotReceive().GetMaxArticlesPerBox(Arg.Any<Dimensions>(), Arg.Any<Dimensions>());
    }
}
