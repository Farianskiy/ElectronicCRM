namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record BulkUpdateCatalogImportRowRequest(
    Guid RowId,
    string? Name,
    string? Article,
    Guid? ProductTypeId,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string>? Characteristics);
