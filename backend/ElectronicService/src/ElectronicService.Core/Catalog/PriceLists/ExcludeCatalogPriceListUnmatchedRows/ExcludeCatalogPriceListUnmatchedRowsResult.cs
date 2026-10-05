using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.ExcludeCatalogPriceListUnmatchedRows;

public sealed record ExcludeCatalogPriceListUnmatchedRowsResult(
    Guid PriceListId,
    CatalogPriceListStatus PriceListStatus,
    int ExcludedRowsCount,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount);
