using System.Collections.ObjectModel;

namespace Cubing.Core.Packing.Exceptions;

/// <summary>Thrown when a cubing request is invalid or cannot be satisfied. No boxes are created in that case.</summary>
public sealed class CubingValidationException(IDictionary<string, string[]> errors)
    : Exception("The cubing request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } =
        new ReadOnlyDictionary<string, string[]>(errors);
}
