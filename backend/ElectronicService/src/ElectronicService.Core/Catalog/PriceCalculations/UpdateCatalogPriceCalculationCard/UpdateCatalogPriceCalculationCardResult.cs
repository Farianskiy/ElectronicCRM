namespace ElectronicService.Core.Catalog.PriceCalculations.UpdateCatalogPriceCalculationCard;

public sealed record UpdateCatalogPriceCalculationCardResult(
    Guid CalculationId,
    string? CustomerName,
    string? ObjectName,
    string? ProjectNumber,
    string? ResponsibleName,
    string? Comment,
    DateOnly? ValidUntil,
    DateTime? UpdatedAtUtc);