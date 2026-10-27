namespace ElectronicService.Contracts.Catalog
    .ImportBatches;

public sealed record GetCatalogImportRowsResponse(
    IReadOnlyCollection<
        CatalogImportRowResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogImportRowResponse(
    Guid RowId,
    int RowNumber,
    string Status,
    IReadOnlyDictionary<int, string> RawData,
    CatalogImportNormalizedRowResponse Data,
    IReadOnlyCollection<
        CatalogImportRowIssueResponse> Issues,
    IReadOnlyCollection<
        CatalogImportRowIssueResponse> Warnings);

public sealed record CatalogImportNormalizedRowResponse(
    string? Name,
    string? Article,
    string? Manufacturer,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string> Characteristics,
    IReadOnlyDictionary<string, CatalogImportCharacteristicOriginResponse> CharacteristicOrigins,
    Guid? ProductTypeId,
    string? ProductTypeResolutionSource,
    decimal? ProductTypeResolutionConfidence);

public sealed record CatalogImportCharacteristicOriginResponse(
    string Source,
    decimal? Confidence,
    string? RawValue,
    Guid? AnalogyConfigurationId,
    int? AnalogyConfigurationVersion,
    IReadOnlyCollection<Guid> AnalogySupportingProductIds,
    string? AnalogyConfigurationHash,
    string? AnalogyCandidateSnapshotId,
    DateTime? AnalogyCandidateSnapshotCreatedAtUtc,
    string? AnalogyFamilyAnchor,
    int? AnalogySupportingProductCount,
    IReadOnlyCollection<Guid> TrainingExampleIds,
    IReadOnlyCollection<Guid> TrainingSourceRowIds);

public sealed record CatalogImportRowIssueResponse(
    string Code,
    string Message,
    string? Field,
    int? SourceColumnNumber);
