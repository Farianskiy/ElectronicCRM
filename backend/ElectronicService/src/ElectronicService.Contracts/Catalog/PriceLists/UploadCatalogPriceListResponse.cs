namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record UploadCatalogPriceListResponse(
    Guid PriceListId,
    Guid ManufacturerId,
    string OriginalFileName,
    long FileSizeBytes,
    string Status);