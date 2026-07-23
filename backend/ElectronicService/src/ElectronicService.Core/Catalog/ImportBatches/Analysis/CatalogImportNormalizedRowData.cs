namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportNormalizedRowData(
    string? Name,
    string? Article,
    string? Manufacturer,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string>
        Characteristics);