namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record GetCatalogPriceListIssueGroupsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<CatalogPriceListIssueGroupResponse> Items);

public sealed record CatalogPriceListIssueGroupResponse(
    string GroupKey,
    string IssueCode,
    string Field,
    string SourceValue,
    int RowsCount,
    IReadOnlyList<int> ExampleRowNumbers);