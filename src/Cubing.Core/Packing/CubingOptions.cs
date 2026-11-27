namespace Cubing.Core.Packing;

/// <summary>Settings for the cubing use case. Bound from the <c>Cubing</c> configuration section by the host.</summary>
public sealed class CubingOptions
{
    public const string SectionName = "Cubing";

    /// <summary>The built-in customer, registered by default.</summary>
    public const string SuperNiceCubes = "SuperNiceCubes";
    
    public const string SuperSafelyPackedCubes = "SuperSafelyPackedCubes";

    /// <summary>Customer used when a request does not name one.</summary>
    public string DefaultCustomerId { get; set; } = SuperNiceCubes;
}
