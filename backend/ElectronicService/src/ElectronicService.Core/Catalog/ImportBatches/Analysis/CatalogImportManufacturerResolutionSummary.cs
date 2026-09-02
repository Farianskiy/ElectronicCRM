namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportManufacturerResolutionSummary(
    int RowsWithManufacturerValueCount,
    int ResolvedByExactNameRowsCount,
    int ResolvedByApprovedAliasRowsCount,
    int IgnoredNoiseRowsCount,
    int UnresolvedRowsCount,
    IReadOnlyCollection<CatalogImportManufacturerResolutionGroup> Groups);