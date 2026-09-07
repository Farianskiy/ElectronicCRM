using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListRows;

public sealed record GetCatalogPriceListRowsQuery(
    Guid PriceListId,
    Guid CurrentUserId,
    CatalogPriceListRowStatus? Status,
    CatalogPriceListRowMatchStatus? MatchStatus,
    string? IssueCode,
    int Page,
    int PageSize);