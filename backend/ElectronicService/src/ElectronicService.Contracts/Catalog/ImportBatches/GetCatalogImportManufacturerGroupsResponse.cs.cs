namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportManufacturerGroupsResponse(
    Guid BatchId,
    uint BatchVersion,
    IReadOnlyCollection<CatalogImportManufacturerGroupResponse> Items);

public sealed record CatalogImportManufacturerGroupResponse(
    string GroupKey,
    string SourceValue,
    string ResolutionSource,
    string? ResolvedManufacturerName,
    int ExactNameRowsCount,
    int ApprovedAliasRowsCount,
    int IgnoredNoiseRowsCount,
    int UnresolvedRowsCount,
    int ManualRowsCount,
    int RowsCount);