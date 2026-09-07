namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record CreateCatalogPriceCalculationResponse(
    Guid CalculationId,
    Guid CreatedByUserId,
    string Title,
    string Currency,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc);