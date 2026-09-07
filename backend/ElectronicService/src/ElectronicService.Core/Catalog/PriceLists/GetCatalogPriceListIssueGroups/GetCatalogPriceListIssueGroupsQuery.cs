namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListIssueGroups;

public sealed record GetCatalogPriceListIssueGroupsQuery(
    Guid PriceListId,
    Guid CurrentUserId,
    string? IssueCode,
    int Page,
    int PageSize);