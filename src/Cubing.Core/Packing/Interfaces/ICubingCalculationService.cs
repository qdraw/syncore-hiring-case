using Cubing.Core.Entities;
using Cubing.Core.Packing.Exceptions;
using Cubing.Core.Packing.Models;

namespace Cubing.Core.Packing.Interfaces;

/// <summary>The "cube and store" use case: calculates the boxes for a request and persists the result.</summary>
public interface ICubingCalculationService
{
    /// <summary>
    /// Applies the default customer when the request names none, cubes the request and stores the calculation.
    /// Nothing is stored when the request is invalid.
    /// </summary>
    /// <exception cref="CubingValidationException">The request is invalid or the article does not fit the box.</exception>
    Task<CubingCalculation> CalculateAndStoreAsync(CubingRequest request, CancellationToken cancellationToken = default);

    Task<CubingCalculation?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
