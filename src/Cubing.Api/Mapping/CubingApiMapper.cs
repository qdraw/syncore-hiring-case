using Cubing.Api.Contracts;
using Cubing.Core.Entities;
using Cubing.Core.Packing.Models;

namespace Cubing.Api.Mapping;

/// <summary>Translates between the HTTP contracts and the domain.</summary>
public static class CubingApiMapper
{
    /// <summary>A missing customer id stays empty; the use case applies the configured default.</summary>
    public static CubingRequest ToDomain(CubingApiRequest r) => new(
        r.CustomerId?.Trim() ?? string.Empty,
        r.Quantity,
        r.ArticleName ?? string.Empty,
        ToDomain(r.ArticleDimensions),
        r.BoxTypeName ?? string.Empty,
        ToDomain(r.BoxDimensions));

    public static CubingApiResponse ToResponse(CubingCalculation c) => new()
    {
        CalculationId = c.Id,
        CustomerId = c.CustomerId,
        ArticleName = c.ArticleName,
        BoxTypeName = c.BoxTypeName,
        TotalQuantity = c.TotalQuantity,
        TotalBoxes = c.Boxes.Count,
        Boxes = c.Boxes
            .OrderBy(b => b.Id)
            .Select(b => new BoxResponse { LicensePlateNumber = b.LicensePlateNumber, NumberOfItems = b.NumberOfItems })
            .ToList(),
    };

    // A missing dimensions object becomes 0x0x0, which the service rejects with a validation error.
    private static Dimensions ToDomain(DimensionsRequest? d) =>
        d is null ? new Dimensions(0, 0, 0) : new Dimensions(d.Length, d.Width, d.Height);
}
