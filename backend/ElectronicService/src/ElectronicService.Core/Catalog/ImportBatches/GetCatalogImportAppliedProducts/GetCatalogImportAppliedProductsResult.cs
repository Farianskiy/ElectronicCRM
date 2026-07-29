namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

public sealed record GetCatalogImportAppliedProductsResult(
    Guid BatchId,
    IReadOnlyCollection<CatalogImportAppliedProductItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogImportAppliedProductItemResult(
    Guid ProductId,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    DateTime AppliedAtUtc);

public sealed record CatalogImportAppliedProductsReadResult(
    IReadOnlyCollection<CatalogImportAppliedProductItemResult> Items,
    int TotalCount);