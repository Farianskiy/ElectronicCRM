namespace ElectronicService.Contracts.Catalog.Products.StockImport;

public sealed record ImportCatalogStockResponse(
    int ReadRowsCount,
    int MatchedRowsCount,
    int UpdatedProductsCount,
    int SkippedRowsCount,
    IReadOnlyList<ImportCatalogStockIssueResponse> Issues);

public sealed record ImportCatalogStockIssueResponse(
    int RowNumber,
    string Article,
    string Message);
