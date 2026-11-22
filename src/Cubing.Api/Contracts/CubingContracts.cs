namespace Cubing.Api.Contracts;

public class DimensionsRequest
{
    public int Length { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class CubingApiRequest
{
    public int Quantity { get; set; }
    public string? ArticleName { get; set; }
    public DimensionsRequest? ArticleDimensions { get; set; }
    public string? BoxTypeName { get; set; }
    public DimensionsRequest? BoxDimensions { get; set; }
}

public class BoxResponse
{
    public string LicensePlateNumber { get; set; } = string.Empty;
    public int NumberOfItems { get; set; }
}

public class CubingApiResponse
{
    public Guid CalculationId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string BoxTypeName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int TotalBoxes { get; set; }
    public List<BoxResponse> Boxes { get; set; } = [];
}
