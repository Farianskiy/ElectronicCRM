namespace ElectronicService.Core.Catalog.ImportBatches.GetManufacturerGroups;

public sealed record GetCatalogImportManufacturerGroupsQuery(
    Guid BatchId,
    Guid CurrentUserId,
    uint? ExpectedVersion);

public sealed record CatalogImportManufacturerGroupResult(
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

public sealed record GetCatalogImportManufacturerGroupsResult(
    Guid BatchId,
    uint BatchVersion,
    IReadOnlyCollection<CatalogImportManufacturerGroupResult> Items);