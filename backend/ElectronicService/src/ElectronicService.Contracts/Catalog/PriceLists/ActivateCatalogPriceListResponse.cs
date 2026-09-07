namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record ActivateCatalogPriceListResponse(
    Guid PriceListId,
    Guid ManufacturerId,
    string Status,
    DateTime? ActivatedAtUtc,
    Guid? ArchivedPriceListId);