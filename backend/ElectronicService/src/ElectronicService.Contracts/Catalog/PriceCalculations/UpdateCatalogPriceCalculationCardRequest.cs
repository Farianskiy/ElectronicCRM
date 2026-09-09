namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record UpdateCatalogPriceCalculationCardRequest(
    string? CustomerName,
    string? ObjectName,
    string? ProjectNumber,
    string? ResponsibleName,
    string? Comment,
    DateOnly? ValidUntil);