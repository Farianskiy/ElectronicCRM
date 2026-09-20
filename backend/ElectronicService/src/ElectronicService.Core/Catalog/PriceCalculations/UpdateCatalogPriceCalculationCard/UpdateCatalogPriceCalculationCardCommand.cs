namespace ElectronicService.Core.Catalog.PriceCalculations.UpdateCatalogPriceCalculationCard;

public sealed record UpdateCatalogPriceCalculationCardCommand(
    Guid CalculationId,
    string? CustomerName,
    string? ObjectName,
    string? ProjectNumber,
    string? ResponsibleName,
    string? Comment,
    DateOnly? ValidUntil,
    Guid CurrentUserId);