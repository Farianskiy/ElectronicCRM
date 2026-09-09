namespace ElectronicService.Core.Catalog.PriceCalculations.ExportCatalogPriceCalculation;

public sealed record ExportCatalogPriceCalculationResult(ReadOnlyMemory<byte> Content, string ContentType, string FileName);