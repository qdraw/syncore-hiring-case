using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Services;
using NSubstitute;

namespace Cubing.Core.Tests.Packing.Services;

public class CustomerProfileRegistryTests
{
    private readonly IPackingStrategy _strategy = Substitute.For<IPackingStrategy>();
    private readonly ILpnGenerator _lpn = Substitute.For<ILpnGenerator>();

    [Fact]
    public void Resolves_registered_customer_case_insensitively()
    {
        var registry = new CustomerProfileRegistry().Add("Acme", _strategy, _lpn);

        var profile = registry.Resolve("ACME");

        Assert.NotNull(profile);
        Assert.Same(_strategy, profile.PackingStrategy);
        Assert.Same(_lpn, profile.LpnGenerator);
    }

    [Theory]
    [InlineData("Other")]
    [InlineData("")]
    [InlineData(" ")]
    public void Unknown_customer_resolves_to_null(string customer)
    {
        Assert.Null(new CustomerProfileRegistry().Add("Acme", _strategy, _lpn).Resolve(customer));
    }

    [Fact]
    public void Adding_the_same_customer_twice_throws()
    {
        var registry = new CustomerProfileRegistry().Add("Acme", _strategy, _lpn);

        Assert.Throws<ArgumentException>(() => registry.Add("acme", _strategy, _lpn));
    }
}
