namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public enum CatalogImportProductTypeSuggestionShadowSampleKind
{
    None = 0,
    Conflict = 1,
    NameConflict = 2,
    Suggestion = 3,
    Match = 4,
    Unresolved = 5
}

public sealed record CatalogImportProductTypeSuggestionShadowResult(
    int RowsAnalyzedCount,
    bool HasSelectedProductType,
    Guid? SelectedProductTypeId,
    string? SelectedProductTypeCode,
    string? SelectedProductTypeName,
    int SuggestedRowsCount,
    int MatchesCount,
    int ConflictsCount,
    int SuggestionsCount,
    int NameConflictsCount,
    int UnresolvedCount,
    int DistinctSuggestedProductTypesCount,
    bool HasMixedSuggestedProductTypes,
    bool SamplesTruncated,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowGroup> TypeGroups,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowSample> Samples,
    IReadOnlyCollection<CatalogImportProductNameEvidenceRow> EvidenceRows);

public sealed record CatalogImportProductTypeSuggestionShadowGroup(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int RowsCount,
    int MatchesCount,
    int ConflictsCount,
    int SuggestionsCount,
    decimal HighestConfidence,
    IReadOnlyCollection<int> ExampleRowNumbers);

public sealed record CatalogImportProductTypeSuggestionShadowSample(
    int RowNumber,
    CatalogImportProductTypeSuggestionShadowSampleKind Kind,
    string ProductName,
    Guid? SelectedProductTypeId,
    string? SelectedProductTypeCode,
    string? SelectedProductTypeName,
    Guid? SuggestedProductTypeId,
    string? SuggestedProductTypeCode,
    string? SuggestedProductTypeName,
    decimal? Confidence,
    int? HighestPriority,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowCandidate> Candidates,
    string Details);

public sealed record CatalogImportProductTypeSuggestionShadowCandidate(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int HighestPriority,
    decimal Confidence,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowEvidence> Evidence);

public sealed record CatalogImportProductTypeSuggestionShadowEvidence(
    Guid DictionaryTermId,
    string Phrase,
    string RawValue,
    string NormalizedValue,
    int Priority,
    string Source,
    int StartIndex,
    int Length)
{
    public int EndIndex => StartIndex + Length;
}