namespace ElectronicService.Core.Catalog.Products
    .GetAuditHistory;

public interface IProductAuditHistoryReader
{
    Task<ProductAuditHistoryPageResult?> ReadAsync(
        Guid productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}