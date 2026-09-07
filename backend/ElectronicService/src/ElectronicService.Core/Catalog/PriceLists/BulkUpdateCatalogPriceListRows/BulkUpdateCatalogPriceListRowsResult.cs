using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.BulkUpdateCatalogPriceListRows;

public sealed record BulkUpdateCatalogPriceListRowsResult(
    Guid PriceListId,
    CatalogPriceListStatus PriceListStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    IReadOnlyList<CatalogPriceListRowDetails> Rows);