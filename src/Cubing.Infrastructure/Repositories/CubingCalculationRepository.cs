using Cubing.Core.Entities;
using Cubing.Core.Repositories;
using Cubing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cubing.Infrastructure.Repositories;

public sealed class CubingCalculationRepository(CubingDbContext db) : ICubingCalculationRepository
{
    public async Task AddAsync(CubingCalculation calculation, CancellationToken cancellationToken = default)
    {
        db.CubingCalculations.Add(calculation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<CubingCalculation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.CubingCalculations
            .AsNoTracking()
            .Include(c => c.Boxes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
