using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Validation;

/// <summary>
/// The customer-independent rules. Customer-specific rules belong to the packing strategy.
/// Adding a rule means adding one line to the relevant list.
/// </summary>
internal static class CubingRules
{
    /// <summary>Guards against requests that would allocate an unreasonable number of boxes.</summary>
    public const int MaxBoxes = 10_000;

    /// <summary>Rules on the request itself, evaluated before anything is looked up or calculated.</summary>
    public static readonly IReadOnlyList<ValidationRule<CubingRequest>> Input =
    [
        new(nameof(CubingRequest.Quantity), r => r.Quantity <= 0, "Quantity must be greater than zero."),
        new(nameof(CubingRequest.ArticleName), r => string.IsNullOrWhiteSpace(r.ArticleName), "Article name is required."),
        new(nameof(CubingRequest.BoxTypeName), r => string.IsNullOrWhiteSpace(r.BoxTypeName), "Box type name is required."),
        new(nameof(CubingRequest.BoxDimensions), r => !r.BoxDimensions.IsPositive, "All box dimensions must be greater than zero."),
        new(nameof(CubingRequest.ArticleDimensions), r => !r.ArticleDimensions.IsPositive, "All article dimensions must be greater than zero."),
    ];

    /// <summary>Rules on the outcome of the customer's packing strategy.</summary>
    public static readonly IReadOnlyList<ValidationRule<PackingCapacity>> Capacity =
    [
        new(nameof(CubingRequest.ArticleDimensions), c => c.ArticlesPerBox <= 0, "The article does not fit inside the box."),
        new(nameof(CubingRequest.Quantity), c => c.ArticlesPerBox > 0 && c.BoxesNeeded > MaxBoxes,
            $"The request requires more than {MaxBoxes} boxes."),
    ];
}

/// <summary>How many articles fit in one box for a given quantity, and the number of boxes that implies.</summary>
internal readonly record struct PackingCapacity(int Quantity, long ArticlesPerBox)
{
    /// <summary>Ceiling division on <c>long</c> values, so nothing overflows. Only meaningful when <see cref="ArticlesPerBox"/> is positive.</summary>
    public long BoxesNeeded => Quantity / ArticlesPerBox + (Quantity % ArticlesPerBox > 0 ? 1 : 0);
}
