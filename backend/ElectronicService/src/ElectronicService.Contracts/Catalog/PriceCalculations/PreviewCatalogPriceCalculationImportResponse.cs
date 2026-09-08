namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record PreviewCatalogPriceCalculationImportResponse(
    int ReadRowsCount,
    int MatchedRowsCount,
    int SkippedRowsCount,
    IReadOnlyList<
        CatalogPriceCalculationImportPreviewRowResponse> Rows);

public sealed record CatalogPriceCalculationImportPreviewRowResponse(
    int RowNumber,
    string Article,
    string? SourceName,
    string? SourceManufacturer,
    decimal? Quantity,
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