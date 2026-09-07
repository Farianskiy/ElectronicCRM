using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationRepository
    : ICatalogPriceCalculationRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceCalculationRepository(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public void Add(
        CatalogPriceCalculation calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);

        _dbContext.CatalogPriceCalculations.Add(
            calculation);
    }

    public Task<CatalogPriceCalculation?> GetByIdAsync(
        Guid calculationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogPriceCalculations
            .Include(calculation =>
                calculation.Lines)
            .Include(calculation =>
                calculation.ManufacturerDiscounts)
            .SingleOrDefaultAsync(
                calculation =>
                    calculation.Id == calculationId,
                cancellationToken);
    }
}