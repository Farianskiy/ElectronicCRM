using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.Abstractions;

public interface ICatalogPriceCalculationRepository
{
    void Add(
        CatalogPriceCalculation calculation);

    Task<CatalogPriceCalculation?> GetByIdAsync(
        Guid calculationId,
        CancellationToken cancellationToken = default);
}