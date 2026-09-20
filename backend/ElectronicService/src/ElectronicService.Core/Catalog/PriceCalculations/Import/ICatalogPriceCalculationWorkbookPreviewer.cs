namespace ElectronicService.Core.Catalog.PriceCalculations.Import;

public interface ICatalogPriceCalculationWorkbookPreviewer
{
    Task<CatalogPriceCalculationImportPreview> PreviewAsync(
        Stream workbookStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public enum CatalogPriceCalculationImportRowStatus
{
    Matched,
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
    IReadOnlyList<CatalogPriceCalculationImportPreviewRow> Rows);

public sealed record CatalogPriceCalculationImportPreviewRow(
    int RowNumber,
    string Article,
    string? SourceName,
    string? SourceManufacturer,
    decimal? Quantity,
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