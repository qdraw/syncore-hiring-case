namespace Cubing.Core.Packing.Models;

public sealed class CubingResult(
    string customerId,
    string articleName,
    string boxTypeName,
    int totalQuantity,
    int totalBoxes,
    IReadOnlyList<PackedBox> boxes)
{
    public string CustomerId { get; } = customerId;
    public string ArticleName { get; } = articleName;
    public string BoxTypeName { get; } = boxTypeName;
    public int TotalQuantity { get; } = totalQuantity;
    public int TotalBoxes { get; } = totalBoxes;
    public IReadOnlyList<PackedBox> Boxes { get; } = boxes;
}
