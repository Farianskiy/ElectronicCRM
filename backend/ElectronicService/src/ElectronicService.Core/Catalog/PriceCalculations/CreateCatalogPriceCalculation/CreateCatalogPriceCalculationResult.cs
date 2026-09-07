using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.CreateCatalogPriceCalculation;

public sealed record CreateCatalogPriceCalculationResult(
    Guid CalculationId,
    Guid CreatedByUserId,
    string Title,
    string Currency,
    CatalogPriceCalculationStatus Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc);