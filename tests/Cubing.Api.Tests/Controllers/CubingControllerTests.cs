using Cubing.Api.Tests;
using System.Net;
using System.Net.Http.Json;
using Cubing.Api.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cubing.Api.Tests.Controllers;

public class CubingControllerTests(CubingApiFactory factory) : IClassFixture<CubingApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static DimensionsRequest Dims(int l, int w, int h) => new() { Length = l, Width = w, Height = h };

    private static CubingApiRequest Valid(Action<CubingApiRequest>? tweak = null, int quantity = 28)
    {
        var request = new CubingApiRequest
        {
            Quantity = quantity,
            ArticleName = "Article1",
            ArticleDimensions = Dims(10, 10, 10),
            BoxTypeName = "Box",
            BoxDimensions = Dims(35, 35, 35),
        };
        tweak?.Invoke(request);
        return request;
    }

    [Fact]
    public async Task Valid_request_returns_boxes()
    {
        var response = await _client.PostAsJsonAsync("/api/cubing", Valid());
        var body = await response.Content.ReadFromJsonAsync<CubingApiResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(Guid.Empty, body!.CalculationId);
        Assert.Equal(2, body.TotalBoxes);
        Assert.Equal(28, body.TotalQuantity);
        Assert.Equal("Article1", body.ArticleName);
        Assert.Equal("Box", body.BoxTypeName);
        Assert.Equal([27, 1], body.Boxes.Select(b => b.NumberOfItems));
        Assert.Equal(2, body.Boxes.Select(b => b.LicensePlateNumber).Distinct().Count());
    }

    [Fact]
    public async Task Missing_customer_id_falls_back_to_the_default_customer()
    {
        var body = await (await _client.PostAsJsonAsync("/api/cubing", Valid()))
            .Content.ReadFromJsonAsync<CubingApiResponse>();

        Assert.Equal("SuperNiceCubes", body!.CustomerId);
    }

    [Fact]
    public async Task Customer_id_selects_the_customers_lpn_format()
    {
        var response = await _client.PostAsJsonAsync("/api/cubing",
            Valid(r => r.CustomerId = CubingApiFactory.PrefixedCustomer));
        var body = await response.Content.ReadFromJsonAsync<CubingApiResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(CubingApiFactory.PrefixedCustomer, body!.CustomerId);
        Assert.All(body.Boxes, b => Assert.StartsWith("PFX-", b.LicensePlateNumber));

        var fetched = await _client.GetFromJsonAsync<CubingApiResponse>($"/api/cubing/{body.CalculationId}");
        Assert.Equal(CubingApiFactory.PrefixedCustomer, fetched!.CustomerId);
    }

    [Fact]
    public async Task Unknown_customer_id_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/cubing", Valid(r => r.CustomerId = "Nobody"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>();
        Assert.Contains("CustomerId", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Created_calculation_can_be_fetched_with_identical_boxes()
    {
        var created = await (await _client.PostAsJsonAsync("/api/cubing", Valid()))
            .Content.ReadFromJsonAsync<CubingApiResponse>();

        var fetched = await _client.GetFromJsonAsync<CubingApiResponse>($"/api/cubing/{created!.CalculationId}");

        Assert.Equal(created.TotalBoxes, fetched!.TotalBoxes);
        Assert.Equal(created.Boxes.Select(b => (b.LicensePlateNumber, b.NumberOfItems)),
            fetched.Boxes.Select(b => (b.LicensePlateNumber, b.NumberOfItems)));
    }

    [Fact]
    public async Task Get_unknown_calculation_returns_404()
    {
        var response = await _client.GetAsync($"/api/cubing/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_request_stores_nothing()
    {
        await _client.PostAsJsonAsync("/api/cubing", Valid(quantity: 0));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Cubing.Infrastructure.Data.CubingDbContext>();
        Assert.False(await db.CubingCalculations.AnyAsync(c => c.TotalQuantity == 0));
    }

    public static TheoryData<CubingApiRequest> InvalidRequests => new()
    {
        Valid(quantity: 0),
        Valid(r => r.ArticleDimensions = Dims(10, 10, 0)),
        Valid(r => r.ArticleDimensions = Dims(10, 10, 12)),
        Valid(r => r.BoxDimensions = Dims(35, -1, 35)),
        Valid(r => r.ArticleDimensions = null),
        Valid(r => r.BoxDimensions = Dims(5, 5, 5)),
        Valid(r => r.ArticleName = null),
        Valid(r => r.BoxTypeName = ""),
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_request_returns_400_validation_problem(CubingApiRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/cubing", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>();
        Assert.NotEmpty(problem!.Errors);
    }

    [Fact]
    public async Task Missing_body_returns_400()
    {
        var response = await _client.PostAsync("/api/cubing", new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
