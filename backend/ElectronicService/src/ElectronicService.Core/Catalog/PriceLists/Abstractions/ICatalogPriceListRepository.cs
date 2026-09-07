using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.Abstractions;

public interface ICatalogPriceListRepository
{
    void Add(CatalogPriceList priceList);

    Task<CatalogPriceList?> GetByIdAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceListRow?> GetRowByIdAsync(
        Guid priceListId,
        Guid rowId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogPriceListRow>>
        GetRowsByIdsAsync(
            Guid priceListId,
            IReadOnlyCollection<Guid> rowIds,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>>
        GetProductIdsByManufacturerAsync(
            Guid manufacturerId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default);

    Task<bool> ProductBelongsToManufacturerAsync(
        Guid productId,
        Guid manufacturerId,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceListIssueGroupBatch?>
    GetIssueGroupBatchAsync(
        Guid priceListId,
        string groupKey,
        int take,
        CancellationToken cancellationToken = default);

    Task<UnitResult<DomainError>>
        SaveCorrectionBatchAsync(
            IReadOnlyCollection<CatalogPriceListRow> rows,
            CancellationToken cancellationToken = default);

    Task<Result<Guid?, DomainError>> ActivateAsync(
        CatalogPriceList priceList,
        CancellationToken cancellationToken = default);

    Task<UnitResult<DomainError>>
        SaveCorrectionAndRefreshStatisticsAsync(
            CatalogPriceList priceList,
            CancellationToken cancellationToken = default);
}

public sealed record CatalogPriceListIssueGroupBatch(
    string IssueCode,
    string Field,
    string SourceValue,
    IReadOnlyList<Guid> RowIds);