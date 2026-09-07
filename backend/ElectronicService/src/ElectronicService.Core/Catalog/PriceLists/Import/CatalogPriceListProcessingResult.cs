using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.Import;

public sealed record CatalogPriceListProcessingResult(
    Guid PriceListId,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    CatalogPriceListStatus Status);