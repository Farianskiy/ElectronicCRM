namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportNormalizedRowData(
    string? Name,
    string? Article,
    string? Manufacturer,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string> Characteristics,
    Guid? ManufacturerId = null,
    string? ManufacturerResolutionSource = null,
    Guid? ManufacturerAliasId = null,
    IReadOnlyDictionary<string, CatalogImportCharacteristicValueOrigin>? CharacteristicOrigins = null,
    Guid? ProductTypeId = null,
    string? ProductTypeResolutionSource = null,
    decimal? ProductTypeResolutionConfidence = null);