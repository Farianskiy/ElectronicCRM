namespace ElectronicService.Core.Catalog.PriceCalculations.Import;

public interface ICatalogPriceCalculationWorkbookPreviewer
{
    Task<CatalogPriceCalculationImportPreview> PreviewAsync(
        Guid calculationId,
        Stream workbookStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public enum CatalogPriceCalculationImportRowStatus
{
    New,
    QuantityChanged,
    Removed,
    Unchanged,
    Invalid,
    ProductNotFound,
    ProductAmbiguous,
    ActivePriceNotFound,
    ActivePriceAmbiguous
}

public sealed record CatalogPriceCalculationImportPreview(
    int ReadRowsCount,
    int MatchedRowsCount,
    int SkippedRowsCount,
    int AddedRowsCount,
    int UpdatedRowsCount,
    int RemovedRowsCount,
    int UnchangedRowsCount,
    bool IsProjectWorkbook,
    string? Warning,
    IReadOnlyList<CatalogPriceCalculationImportPreviewRow> Rows,
    IReadOnlyList<CatalogPriceCalculationComponentImportPreviewRow> ComponentRows,
    IReadOnlyList<CatalogPriceCalculationCharacteristicImportPreviewRow> CharacteristicRows);

public sealed record CatalogPriceCalculationImportPreviewRow(
    int RowNumber,
    string Article,
    string? SourceName,
    string? SourceManufacturer,
    decimal? Quantity,
    Guid? ExistingLineId,
    decimal? CurrentQuantity,
    CatalogPriceCalculationImportRowStatus Status,
    string? Message,
    Guid? ProductId,
    string? ProductArticle,
    string? ProductName,
    Guid? ManufacturerId,
    string? ManufacturerName,
    decimal? StockQuantity,
    decimal? ShortageQuantity,
    Guid? PriceListId,
    Guid? PriceListRowId,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount);

public enum CatalogPriceCalculationComponentImportRowStatus
{
    New = 0,
    QuantityChanged = 1,
    Removed = 2,
    Unchanged = 3,
    Invalid = 4,
    MainLineNotFound = 5,
    ComponentNotFound = 6,
    NeedNotFound = 7,
    ActivePriceNotFound = 8,
    ActivePriceAmbiguous = 9
}

public sealed record CatalogPriceCalculationComponentImportPreviewRow(
    int RowNumber,
    string MainProductArticle,
    string NeedName,
    string ComponentArticle,
    int? QuantityPerUnit,
    Guid? MainLineId,
    Guid? ExistingComponentLineId,
    Guid? NeedDefinitionId,
    Guid? ComponentProductId,
    int? CurrentQuantityPerUnit,
    CatalogPriceCalculationComponentImportRowStatus Status,
    string? Message);

public enum CatalogPriceCalculationCharacteristicImportRowStatus
{
    Changed = 0,
    Removed = 1,
    Unchanged = 2,
    Invalid = 3,
    ProductNotFound = 4,
    CharacteristicNotFound = 5
}

public sealed record CatalogPriceCalculationCharacteristicImportPreviewRow(
    int RowNumber,
    Guid? ProductId,
    string Article,
    string ProductName,
    string ProductTypeName,
    string CharacteristicCode,
    string CharacteristicName,
    string DataType,
    string? Unit,
    bool IsRequired,
    string? CurrentValue,
    string? NewValue,
    CatalogPriceCalculationCharacteristicImportRowStatus Status,
    string? Message);
