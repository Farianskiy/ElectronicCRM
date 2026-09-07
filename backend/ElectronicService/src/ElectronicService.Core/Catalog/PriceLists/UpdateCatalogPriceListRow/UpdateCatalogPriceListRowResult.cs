using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.UpdateCatalogPriceListRow;

public sealed record UpdateCatalogPriceListRowResult(
    Guid PriceListId,
    CatalogPriceListStatus PriceListStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    CatalogPriceListRowDetails Row);