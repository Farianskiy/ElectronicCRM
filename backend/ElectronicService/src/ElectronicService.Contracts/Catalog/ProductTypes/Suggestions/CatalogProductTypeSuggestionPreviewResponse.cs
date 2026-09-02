namespace ElectronicService.Contracts.Catalog.ProductTypes.Suggestions;

public sealed record CatalogProductTypeSuggestionPreviewResponse(
    string ProductName,
    string NormalizedProductName,
    string Status,
    bool IsSuggested,
    bool IsConflict,
    CatalogProductTypeSuggestionCandidateResponse? SelectedCandidate,
    IReadOnlyCollection<CatalogProductTypeSuggestionCandidateResponse> Candidates);

public sealed record CatalogProductTypeSuggestionCandidateResponse(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int HighestPriority,
    decimal Confidence,
    IReadOnlyCollection<CatalogProductTypeSuggestionEvidenceResponse> Evidence);

public sealed record CatalogProductTypeSuggestionEvidenceResponse(
    Guid DictionaryTermId,
    string Phrase,
    string RawValue,
    string NormalizedValue,
    int Priority,
    string Source,
    int StartIndex,
    int Length,
    int EndIndex);