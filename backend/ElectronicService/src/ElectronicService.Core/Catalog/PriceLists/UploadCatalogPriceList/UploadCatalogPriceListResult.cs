using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.UploadCatalogPriceList;

public sealed record UploadCatalogPriceListResult(
    Guid PriceListId,
    Guid ManufacturerId,
    string OriginalFileName,
    long FileSizeBytes,
    CatalogPriceListStatus Status);