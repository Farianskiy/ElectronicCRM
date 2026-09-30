namespace ElectronicService.Core.Catalog.Products.BulkUpdateProducts;

public sealed record BulkUpdateCatalogProductsCommand(
    Guid ChangedByUserId,
    bool CanEditProducts,
    bool CanManagePrices,
    bool CanManageStock,
    IReadOnlyCollection<BulkUpdateCatalogProductItem> Rows);

public sealed record BulkUpdateCatalogProductItem(
    Guid ProductId,
    string? Name,
    string? Article,
    Guid? ManufacturerId,
    decimal? PriceAmount,
    string? PriceCurrency,
    decimal? StockQuantity)
{
    public bool ChangesGeneralInformation =>
        Name is not null || Article is not null || ManufacturerId.HasValue;

    public bool ChangesPrice =>
        PriceAmount.HasValue || PriceCurrency is not null;

    public bool ChangesStock => StockQuantity.HasValue;

    public bool HasChanges =>
        ChangesGeneralInformation || ChangesPrice || ChangesStock;
}
