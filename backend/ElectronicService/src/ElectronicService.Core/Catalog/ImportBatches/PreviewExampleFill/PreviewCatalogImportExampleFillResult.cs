namespace ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;

public sealed record PreviewCatalogImportExampleFillResult(
    uint Version,
    int ExamplesCount,
    int TargetRowsCount,
    int ReadyRowsCount,
    int ReviewRowsCount,
    int ConflictRowsCount,
    int UnmatchedRowsCount,
    IReadOnlyCollection<PreviewCatalogImportExampleFillRowResult> Rows);

public sealed record PreviewCatalogImportExampleFillRowResult(
    Guid RowId,
    int RowNumber,
    string ProductName,
    string State,
    string? Reason,
    IReadOnlyCollection<PreviewCatalogImportExampleFillProposalResult> Proposals,
    IReadOnlyCollection<PreviewCatalogImportExampleFillConflictResult> Conflicts);

public sealed record PreviewCatalogImportExampleFillProposalResult(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string ProposedValue,
    string? CurrentValue,
    string MatchedRawValue,
    decimal Confidence,
    IReadOnlyCollection<Guid> SourceExampleIds,
    IReadOnlyCollection<Guid> SourceRowIds);

public sealed record PreviewCatalogImportExampleFillConflictResult(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Reason,
    string? CurrentValue,
    IReadOnlyCollection<string> ProposedValues,
    IReadOnlyCollection<Guid> SourceExampleIds);
