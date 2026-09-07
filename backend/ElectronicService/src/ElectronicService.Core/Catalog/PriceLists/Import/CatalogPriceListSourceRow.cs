namespace ElectronicService.Core.Catalog.PriceLists.Import;

public sealed record CatalogPriceListSourceRow(
    int RowNumber,
    string Article,
    string Name,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount,
    string? ProductLink,
    string? Unit);