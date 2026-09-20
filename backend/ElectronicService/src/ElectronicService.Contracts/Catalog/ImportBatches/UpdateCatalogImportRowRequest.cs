namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportConfirmedSpanRequest(string ProductName, int Start, int Length);

public sealed record UpdateCatalogImportRowRequest(
    string? Name,
    string? Article,
    Guid? ProductTypeId,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string>? Characteristics,
    IReadOnlyDictionary<Guid, CatalogImportConfirmedSpanRequest>? ConfirmedSpans = null);