namespace ElectronicService.Core.Catalog.Products.StockImport;

public interface ICatalogStockWorkbookImporter
{
    Task<CatalogStockImportResult> ImportAsync(
        Guid manufacturerId,
        Stream workbookStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogStockImportResult(
    int ReadRowsCount,
    int MatchedRowsCount,
    int UpdatedProductsCount,
    int SkippedRowsCount,
    IReadOnlyList<CatalogStockImportIssue> Issues);

public sealed record CatalogStockImportIssue(
    int RowNumber,
    string Article,
    string Message);
