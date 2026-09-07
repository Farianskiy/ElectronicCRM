namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record UpdateCatalogPriceListRowRequest(
    string Article,
    string Name,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount,
    Uri? ProductUrl,
    string? Unit,
    Guid? ProductId);