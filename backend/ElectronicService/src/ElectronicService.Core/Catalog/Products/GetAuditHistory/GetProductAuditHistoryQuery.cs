namespace ElectronicService.Core.Catalog.Products
    .GetAuditHistory;

public sealed record GetProductAuditHistoryQuery(
    Guid ProductId,
    int PageNumber,
    int PageSize);