using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;
using Cubing.Core.Packing.Validation;

namespace Cubing.Core.Packing.Services;

public sealed class CubingService(ICustomerProfileResolver profiles) : ICubingService
{
    private const int MaxLpnAttemptsPerBox = 10;

    public CubingResult Cube(CubingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Each stage validates (no side effects) and only then the next one runs, because it depends on valid
        // input. Nothing is calculated or generated before every check has passed.
        var profile = ValidateRequest(request);
        var capacity = ValidateCapacity(request, profile);

        var boxCount = (int)capacity.BoxesNeeded;
        var boxes = new List<PackedBox>(boxCount);
        var usedLpns = new HashSet<string>(StringComparer.Ordinal);
        var remaining = request.Quantity;

        for (var i = 0; i < boxCount; i++)
        {
            var items = (int)Math.Min(remaining, capacity.ArticlesPerBox);
            boxes.Add(new PackedBox(GenerateUniqueLpn(profile.LpnGenerator, usedLpns), items));
            remaining -= items;
        }

        return new CubingResult(
            request.CustomerId, request.ArticleName, request.BoxTypeName, request.Quantity, boxes.Count, boxes);
    }

    private CustomerProfile ValidateRequest(CubingRequest request)
    {
        var errors = ValidationRule<CubingRequest>.Evaluate(CubingRules.Input, request);

        var profile = profiles.Resolve(request.CustomerId);
        if (profile is null)
        {
            errors[nameof(request.CustomerId)] = [$"Unknown customer '{request.CustomerId}'."];
        }

        if (errors.Count > 0 || profile is null)
        {
            throw new CubingValidationException(errors);
        }

        return profile;
    }

    private static PackingCapacity ValidateCapacity(CubingRequest request, CustomerProfile profile)
    {
        var articleError = profile.PackingStrategy.ValidateArticle(request.ArticleDimensions);
        if (articleError is not null)
        {
            throw new CubingValidationException(
                new Dictionary<string, string[]> { [nameof(request.ArticleDimensions)] = [articleError] });
        }

        var capacity = new PackingCapacity(
            request.Quantity,
            profile.PackingStrategy.GetMaxArticlesPerBox(request.ArticleDimensions, request.BoxDimensions));

        var errors = ValidationRule<PackingCapacity>.Evaluate(CubingRules.Capacity, capacity);
        return errors.Count > 0 ? throw new CubingValidationException(errors) : capacity;
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
