namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportManufacturerResolutionSummaryResponse(
    int RowsWithManufacturerValueCount,
    int ResolvedByExactNameRowsCount,
    int ResolvedByApprovedAliasRowsCount,
    int IgnoredNoiseRowsCount,
    int UnresolvedRowsCount,
    IReadOnlyCollection<CatalogImportManufacturerResolutionGroupResponse> Groups);

public sealed record CatalogImportManufacturerResolutionGroupResponse(
    string SourceValue,
    string NormalizedSourceValue,
    string Status,
    Guid? ManufacturerId,
    string? ResolvedManufacturerName,
    string Source,
    Guid? ManufacturerAliasId,
    Guid? ManufacturerNoisePhraseId,
    string? NoiseReason,
    int OccurrenceCount,
    IReadOnlyCollection<int> ExampleRowNumbers,
    IReadOnlyCollection<string> ExampleProductNames);