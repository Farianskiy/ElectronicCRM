namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record PreviewCatalogImportExampleFillRequest(
    uint ExpectedVersion,
    IReadOnlyCollection<Guid>? ExampleRowIds,
    IReadOnlyCollection<Guid>? TargetRowIds);

public sealed record PreviewCatalogImportExampleFillResponse(
    uint Version,
    int ExamplesCount,
    int TargetRowsCount,
    int ReadyRowsCount,
    int ReviewRowsCount,
    int ConflictRowsCount,
    int UnmatchedRowsCount,
    IReadOnlyCollection<PreviewCatalogImportExampleFillRowResponse> Rows);

public sealed record PreviewCatalogImportExampleFillRowResponse(
    Guid RowId,
    int RowNumber,
    string ProductName,
    string State,
    IReadOnlyCollection<PreviewCatalogImportExampleFillProposalResponse> Proposals,
    IReadOnlyCollection<PreviewCatalogImportExampleFillConflictResponse> Conflicts);

public sealed record PreviewCatalogImportExampleFillProposalResponse(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string ProposedValue,
    string? CurrentValue,
    string MatchedRawValue,
    decimal Confidence,
    IReadOnlyCollection<Guid> SourceExampleIds,
    IReadOnlyCollection<Guid> SourceRowIds);

public sealed record PreviewCatalogImportExampleFillConflictResponse(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Reason,
    string? CurrentValue,
    IReadOnlyCollection<string> ProposedValues,
    IReadOnlyCollection<Guid> SourceExampleIds);
