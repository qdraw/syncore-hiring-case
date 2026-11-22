using Cubing.Core.Entities;
using Cubing.Core.Packing;
using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;
using Cubing.Core.Packing.Services;
using Cubing.Core.Repositories;
using NSubstitute;

namespace Cubing.Core.Tests.Packing.Services;

public class CubingCalculationServiceTests
{
    private readonly ICubingService _cubing = Substitute.For<ICubingService>();
    private readonly ICubingCalculationRepository _repository = Substitute.For<ICubingCalculationRepository>();
    private readonly CubingCalculationService _sut;

    public CubingCalculationServiceTests()
    {
        _cubing.Cube(Arg.Any<CubingRequest>()).Returns(call =>
        {
            var r = call.Arg<CubingRequest>();
            return new CubingResult(r.CustomerId, r.ArticleName, r.BoxTypeName, r.Quantity, 1,
                [new PackedBox("LPN-1", r.Quantity)]);
        });
        _sut = new CubingCalculationService(_cubing, _repository, new CubingOptions { DefaultCustomerId = "Default" });
    }

    private static CubingRequest Request(string customerId) =>
        new(customerId, 5, "Article1", new Dimensions(10, 10, 10), "Box", new Dimensions(35, 35, 35));

    [Fact]
    public async Task Stores_and_returns_the_calculation()
    {
        var calculation = await _sut.CalculateAndStoreAsync(Request("Acme"));

        Assert.NotEqual(Guid.Empty, calculation.Id);
        Assert.Equal("Acme", calculation.CustomerId);
        Assert.Equal(5, calculation.TotalQuantity);
        Assert.Equal("LPN-1", Assert.Single(calculation.Boxes).LicensePlateNumber);
        await _repository.Received(1).AddAsync(calculation, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_customer_id_falls_back_to_the_default_customer(string customerId)
    {
        var calculation = await _sut.CalculateAndStoreAsync(Request(customerId));

        Assert.Equal("Default", calculation.CustomerId);
        _cubing.Received(1).Cube(Arg.Is<CubingRequest>(r => r.CustomerId == "Default"));
    }

    [Fact]
    public async Task Invalid_request_stores_nothing()
    {
        _cubing.Cube(Arg.Any<CubingRequest>())
            .Returns(_ => throw new CubingValidationException(new Dictionary<string, string[]>()));

        await Assert.ThrowsAsync<CubingValidationException>(() => _sut.CalculateAndStoreAsync(Request("Acme")));

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Get_delegates_to_the_repository()
    {
        var id = Guid.NewGuid();
        var stored = new CubingCalculation { Id = id };
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(stored);

        Assert.Same(stored, await _sut.GetAsync(id));
    }
}
