namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record GetCatalogPriceListRowsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<GetCatalogPriceListRowResponse> Items);

public sealed record GetCatalogPriceListRowResponse(
    Guid RowId,
    int RowNumber,
    string Article,
    string Name,
    decimal? BasePriceAmount,
    decimal? MrcPriceAmount,
    Uri? ProductUrl,
    string? Unit,
    Guid? ProductId,
    string Status,
    string MatchStatus,
    decimal? MatchConfidencePercent,
    IReadOnlyList<CatalogPriceListRowIssueResponse> Issues);

public sealed record CatalogPriceListRowIssueResponse(
    string Code,
    string Field,
    string Message);