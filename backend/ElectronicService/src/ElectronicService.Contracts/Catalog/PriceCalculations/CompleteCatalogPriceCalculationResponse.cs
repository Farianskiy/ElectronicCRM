namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record CompleteCatalogPriceCalculationResponse(
    Guid CalculationId,
    string Title,
    string Currency,
    string Status,
    int LinesCount,
    int ManufacturersCount,
    decimal TotalAmount,
    DateTime? CompletedAtUtc);