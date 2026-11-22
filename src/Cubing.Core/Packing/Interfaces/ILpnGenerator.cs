namespace Cubing.Core.Packing.Interfaces;

/// <summary>Generates license plate numbers; formats are customer specific (GUID, prefixed, sequential, ...).</summary>
public interface ILpnGenerator
{
    string Generate();
}
