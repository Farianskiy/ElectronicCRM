using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.CompleteCatalogPriceCalculation;

public sealed record CompleteCatalogPriceCalculationResult(
    Guid CalculationId,
    string Title,
    string Currency,
    CatalogPriceCalculationStatus Status,
    int LinesCount,
    int ManufacturersCount,
    decimal TotalAmount,
    DateTime? CompletedAtUtc);