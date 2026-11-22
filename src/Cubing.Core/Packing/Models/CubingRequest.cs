namespace Cubing.Core.Packing.Models;

public sealed class CubingRequest(
    string customerId,
    int quantity,
    string articleName,
    Dimensions articleDimensions,
    string boxTypeName,
    Dimensions boxDimensions)
{
    public string CustomerId { get; } = customerId;
    public int Quantity { get; } = quantity;
    public string ArticleName { get; } = articleName;
    public Dimensions ArticleDimensions { get; } = articleDimensions;
    public string BoxTypeName { get; } = boxTypeName;
    public Dimensions BoxDimensions { get; } = boxDimensions;
}
