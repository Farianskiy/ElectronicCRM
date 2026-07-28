namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record UpdateCatalogImportRowRequest(
    string? Name,
    string? Article,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string>? Characteristics);