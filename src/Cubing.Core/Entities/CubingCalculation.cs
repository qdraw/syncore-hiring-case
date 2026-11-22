namespace Cubing.Core.Entities;

/// <summary>A stored cubing calculation: which article/box was packed and the resulting boxes.</summary>
public class CubingCalculation
{
    public Guid Id { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string BoxTypeName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<CalculatedBox> Boxes { get; set; } = [];
}

public class CalculatedBox
{
    public int Id { get; set; }
    public Guid CubingCalculationId { get; set; }
    public string LicensePlateNumber { get; set; } = string.Empty;
    public int NumberOfItems { get; set; }
}
