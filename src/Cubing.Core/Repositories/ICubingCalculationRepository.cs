using Cubing.Core.Entities;

namespace Cubing.Core.Repositories;

public interface ICubingCalculationRepository
{
    /// <summary>Persists the calculation (including its boxes).</summary>
    Task AddAsync(CubingCalculation calculation, CancellationToken cancellationToken = default);

    Task<CubingCalculation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
