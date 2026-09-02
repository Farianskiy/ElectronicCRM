namespace ElectronicService.Core.Catalog.ImportBatches.BulkUpdateCatalogImportRows;

public sealed record BulkUpdateCatalogImportRowsCommand(
    Guid BatchId,
    Guid CurrentUserId,
    uint ExpectedVersion,
    IReadOnlyCollection<BulkUpdateCatalogImportRowItem> Rows);

public sealed record BulkUpdateCatalogImportRowItem(
    Guid RowId,
    string? Name,
    string? Article,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string> Characteristics);