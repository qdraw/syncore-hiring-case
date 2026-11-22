using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Services;

public sealed class CubingService(ICustomerProfileResolver profiles) : ICubingService
{
    /// <summary>Guards against requests that would allocate an unreasonable number of boxes.</summary>
    private const int MaxBoxes = 10_000;

    private const int MaxLpnAttemptsPerBox = 10;

    public CubingResult Cube(CubingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (profile, capacity, boxCount) = Validate(request);

        var boxes = new List<PackedBox>(boxCount);
        var usedLpns = new HashSet<string>(StringComparer.Ordinal);
        var remaining = request.Quantity;

        for (var i = 0; i < boxCount; i++)
        {
            var items = (int)Math.Min(remaining, capacity);
            boxes.Add(new PackedBox(GenerateUniqueLpn(profile.LpnGenerator, usedLpns), items));
            remaining -= items;
        }

        return new CubingResult(
            request.CustomerId, request.ArticleName, request.BoxTypeName, request.Quantity, boxes.Count, boxes);
    }

    private (CustomerProfile Profile, long Capacity, int BoxCount) Validate(CubingRequest request)
    {
        var errors = ValidateInput(request);

        var profile = profiles.Resolve(request.CustomerId);
        if (profile is null)
        {
            errors[nameof(request.CustomerId)] = [$"Unknown customer '{request.CustomerId}'."];
        }

        long capacity = 0;
        var boxCount = 0;
        if (errors.Count == 0 && profile is not null)
        {
            var articleError = profile.PackingStrategy.ValidateArticle(request.ArticleDimensions);
            if (articleError is not null)
            {
                errors[nameof(request.ArticleDimensions)] = [articleError];
            }
            else
            {
                capacity = profile.PackingStrategy.GetMaxArticlesPerBox(request.ArticleDimensions, request.BoxDimensions);
                boxCount = CalculateBoxCount(request.Quantity, capacity, errors);
            }
        }

        if (errors.Count > 0 || profile is null)
        {
            throw new CubingValidationException(errors);
        }

        return (profile, capacity, boxCount);
    }

    /// <summary>Customer-independent checks only; customer-specific rules belong to the packing strategy.</summary>
    private static Dictionary<string, string[]> ValidateInput(CubingRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Quantity <= 0)
            errors[nameof(request.Quantity)] = ["Quantity must be greater than zero."];
        if (string.IsNullOrWhiteSpace(request.ArticleName))
            errors[nameof(request.ArticleName)] = ["Article name is required."];
        if (string.IsNullOrWhiteSpace(request.BoxTypeName))
            errors[nameof(request.BoxTypeName)] = ["Box type name is required."];
        if (!request.BoxDimensions.IsPositive)
            errors[nameof(request.BoxDimensions)] = ["All box dimensions must be greater than zero."];
        if (!request.ArticleDimensions.IsPositive)
            errors[nameof(request.ArticleDimensions)] = ["All article dimensions must be greater than zero."];

        return errors;
    }

    /// <summary>Returns the number of boxes needed, or 0 after recording an error when the request cannot be packed.</summary>
    private static int CalculateBoxCount(int quantity, long capacity, Dictionary<string, string[]> errors)
    {
        if (capacity <= 0)
        {
            errors[nameof(CubingRequest.ArticleDimensions)] = ["The article does not fit inside the box."];
            return 0;
        }

        // Overflow-safe ceiling division on long values.
        var boxes = quantity / capacity + (quantity % capacity > 0 ? 1 : 0);
        if (boxes > MaxBoxes)
        {
            errors[nameof(CubingRequest.Quantity)] = [$"The request requires more than {MaxBoxes} boxes."];
            return 0;
        }

        return (int)boxes;
    }

    private static string GenerateUniqueLpn(ILpnGenerator generator, HashSet<string> usedLpns)
    {
        for (var attempt = 0; attempt < MaxLpnAttemptsPerBox; attempt++)
        {
            var lpn = generator.Generate();
            if (!string.IsNullOrWhiteSpace(lpn) && usedLpns.Add(lpn))
            {
                return lpn;
            }
        }

        throw new InvalidOperationException(
            $"Could not generate a unique license plate number after {MaxLpnAttemptsPerBox} attempts.");
    }
}
