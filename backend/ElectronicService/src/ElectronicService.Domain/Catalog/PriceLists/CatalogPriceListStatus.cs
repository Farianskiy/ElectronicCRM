namespace ElectronicService.Domain.Catalog.PriceLists;

public enum CatalogPriceListStatus
{
    None = 0,

    Uploaded = 1,

    Processing = 2,

    NeedsCorrection = 3,

    Ready = 4,

    Active = 5,

    Archived = 6,

    Failed = 7
}