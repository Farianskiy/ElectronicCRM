namespace ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportRow;

public sealed record UpdateCatalogImportRowCommand(
    Guid BatchId,
    Guid RowId,
    Guid CurrentUserId,
    string? Name,
    string? Article,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string> Characteristics);