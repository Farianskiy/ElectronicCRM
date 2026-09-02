namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record CatalogImportProductTypeSuggestionShadowResponse(
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
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowGroupResponse> TypeGroups,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowSampleResponse> Samples);

public sealed record CatalogImportProductTypeSuggestionShadowGroupResponse(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int RowsCount,
    int MatchesCount,
    int ConflictsCount,
    int SuggestionsCount,
    decimal HighestConfidence,
    IReadOnlyCollection<int> ExampleRowNumbers);

public sealed record CatalogImportProductTypeSuggestionShadowSampleResponse(
    int RowNumber,
    string Kind,
    string ProductName,
    Guid? SelectedProductTypeId,
    string? SelectedProductTypeCode,
    string? SelectedProductTypeName,
    Guid? SuggestedProductTypeId,
    string? SuggestedProductTypeCode,
    string? SuggestedProductTypeName,
    decimal? Confidence,
    int? HighestPriority,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowCandidateResponse> Candidates,
    string Details);

public sealed record CatalogImportProductTypeSuggestionShadowCandidateResponse(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int HighestPriority,
    decimal Confidence,
    IReadOnlyCollection<CatalogImportProductTypeSuggestionShadowEvidenceResponse> Evidence);

public sealed record CatalogImportProductTypeSuggestionShadowEvidenceResponse(
    Guid DictionaryTermId,
    string Phrase,
    string RawValue,
    string NormalizedValue,
    int Priority,
    string Source,
    int StartIndex,
    int Length,
    int EndIndex);