using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.ActivateCatalogPriceList;

public sealed record ActivateCatalogPriceListResult(
    Guid PriceListId,
    Guid ManufacturerId,
    CatalogPriceListStatus Status,
    DateTime? ActivatedAtUtc,
    Guid? ArchivedPriceListId);