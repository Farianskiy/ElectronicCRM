namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetFieldComparison(
    Guid CharacteristicDefinitionId,
    string Name,
    string? Unit,
    string? CurrentValue,
    string? ProposedValue,
    string Status);

public sealed record CatalogRecognitionRuleSetBatchPreviewItem(
    Guid RowId,
    int RowNumber,
    string? ProductName,
    string Status,
    CatalogRecognitionRuleSetNamePreviewResult? Preview)
{
    public IReadOnlyList<CatalogRecognitionRuleSetFieldComparison>
        Comparisons
    { get; init; } =
            Array.Empty<CatalogRecognitionRuleSetFieldComparison>();
}

public sealed record CatalogRecognitionRuleSetBatchPreviewPage(
    Guid BatchId,
    Guid VersionId,
    int VersionNumber,
    DateTime CheckedAtUtc,
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<CatalogRecognitionRuleSetBatchPreviewItem> Items)
{
    public int ProposedRowsCount => Items.Count(item =>
        string.Equals(item.Status, "Proposed", StringComparison.Ordinal));

    public int ConflictRowsCount => Items.Count(item =>
        string.Equals(item.Status, "Conflict", StringComparison.Ordinal));

    public int NoMatchRowsCount => Items.Count(item =>
        string.Equals(item.Status, "NoMatch", StringComparison.Ordinal));

    public int OutsideScopeRowsCount => Items.Count(item =>
        string.Equals(item.Status, "OutsideScope", StringComparison.Ordinal));
}