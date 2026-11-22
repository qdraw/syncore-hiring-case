namespace Cubing.Core.Packing.Models;

public sealed class PackedBox(string licensePlateNumber, int numberOfItems)
{
    public string LicensePlateNumber { get; } = licensePlateNumber;
    public int NumberOfItems { get; } = numberOfItems;
}
