namespace ElectronicService.Domain.Catalog.PriceLists;

public enum CatalogPriceListRowMatchStatus
{
    None = 0,

    Pending = 1,

    MatchedByArticle = 2,

    MatchedByName = 3,

    MatchedManually = 4,

    Ambiguous = 5,

    ProductNotFound = 6
}