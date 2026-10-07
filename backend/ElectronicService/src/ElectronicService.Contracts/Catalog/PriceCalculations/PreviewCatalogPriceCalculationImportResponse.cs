namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record PreviewCatalogPriceCalculationImportResponse(
    int ReadRowsCount,
    int MatchedRowsCount,
    int SkippedRowsCount,
    int AddedRowsCount,
    int UpdatedRowsCount,
    int RemovedRowsCount,
    int UnchangedRowsCount,
    bool IsProjectWorkbook,
    string? Warning,
    IReadOnlyList<
        CatalogPriceCalculationImportPreviewRowResponse> Rows,
    IReadOnlyList<
        CatalogPriceCalculationComponentImportPreviewRowResponse> ComponentRows,
    IReadOnlyList<
        CatalogPriceCalculationCharacteristicImportPreviewRowResponse> CharacteristicRows);

public sealed record CatalogPriceCalculationImportPreviewRowResponse(
    int RowNumber,
    string Article,
    string? SourceName,
    string? SourceManufacturer,
    decimal? Quantity,
    Guid? ExistingLineId,
    decimal? CurrentQuantity,
    string Status,
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

public sealed record CatalogPriceCalculationComponentImportPreviewRowResponse(
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
    string Status,
    string? Message);

public sealed record CatalogPriceCalculationCharacteristicImportPreviewRowResponse(
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
    string Status,
    string? Message);
