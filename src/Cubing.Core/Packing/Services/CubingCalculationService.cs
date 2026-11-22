using Cubing.Core.Entities;
using Cubing.Core.Packing.Interfaces;
using Cubing.Core.Packing.Models;
using Cubing.Core.Repositories;

namespace Cubing.Core.Packing.Services;

public sealed class CubingCalculationService(
    ICubingService cubingService,
    ICubingCalculationRepository repository,
    CubingOptions options) : ICubingCalculationService
{
    public async Task<CubingCalculation> CalculateAndStoreAsync(
        CubingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            request = request.WithCustomerId(options.DefaultCustomerId);
        }

        var result = cubingService.Cube(request);
        var calculation = CubingCalculation.From(result);
        await repository.AddAsync(calculation, cancellationToken);
        return calculation;
    }

    public Task<CubingCalculation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);
}
