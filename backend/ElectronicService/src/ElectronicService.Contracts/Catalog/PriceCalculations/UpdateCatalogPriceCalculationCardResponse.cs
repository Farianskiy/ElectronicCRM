namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record UpdateCatalogPriceCalculationCardResponse(
    Guid CalculationId,
    string? CustomerName,
    string? ObjectName,
    string? ProjectNumber,
    string? ResponsibleName,
    string? Comment,
    DateOnly? ValidUntil,
    DateTime? UpdatedAtUtc);