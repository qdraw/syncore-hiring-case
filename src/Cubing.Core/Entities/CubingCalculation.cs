using Cubing.Core.Packing.Models;

namespace Cubing.Core.Entities;

/// <summary>A stored cubing calculation: which article/box was packed and the resulting boxes.</summary>
public class CubingCalculation
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string ArticleName { get; set; } = string.Empty;
    public string BoxTypeName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<CalculatedBox> Boxes { get; set; } = [];

    /// <summary>Creates a new calculation (with a fresh id) from a cubing result.</summary>
    public static CubingCalculation From(CubingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new CubingCalculation
        {
            Id = Guid.NewGuid(),
            CustomerId = result.CustomerId,
            ArticleName = result.ArticleName,
            BoxTypeName = result.BoxTypeName,
            TotalQuantity = result.TotalQuantity,
            Boxes = result.Boxes
                .Select(b => new CalculatedBox { LicensePlateNumber = b.LicensePlateNumber, NumberOfItems = b.NumberOfItems })
                .ToList(),
        };
    }
}

public class CalculatedBox
{
    public int Id { get; set; }
    public Guid CubingCalculationId { get; set; }
    public string LicensePlateNumber { get; set; } = string.Empty;
    public int NumberOfItems { get; set; }
}
