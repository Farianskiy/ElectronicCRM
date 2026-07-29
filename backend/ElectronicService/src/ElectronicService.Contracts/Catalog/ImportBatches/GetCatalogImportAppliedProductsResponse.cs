namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportAppliedProductsResponse(
    Guid BatchId,
    IReadOnlyCollection<CatalogImportAppliedProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogImportAppliedProductResponse(
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