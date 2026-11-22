using Cubing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cubing.Infrastructure.Data;

public class CubingDbContext(DbContextOptions<CubingDbContext> options) : DbContext(options)
{
    public DbSet<CubingCalculation> CubingCalculations => Set<CubingCalculation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CubingCalculation>(e =>
        {
            e.Property(c => c.CustomerId).HasMaxLength(100).IsRequired();
            e.Property(c => c.ArticleName).HasMaxLength(200).IsRequired();
            e.Property(c => c.BoxTypeName).HasMaxLength(200).IsRequired();
            e.HasMany(c => c.Boxes).WithOne().HasForeignKey(b => b.CubingCalculationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CalculatedBox>(e =>
        {
            e.Property(b => b.LicensePlateNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(b => b.LicensePlateNumber).IsUnique();
        });
    }
}
