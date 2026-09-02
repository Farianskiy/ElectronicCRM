namespace ElectronicService.Core.Catalog.ProductTypes.Suggestions;

public enum CatalogProductTypeSuggestionStatus
{
    None = 0,
    Unresolved = 1,
    Suggested = 2,
    Conflict = 3
}

public sealed record CatalogProductTypeSuggestionResult(
    string ProductName,
    string NormalizedProductName,
    CatalogProductTypeSuggestionStatus Status,
    CatalogProductTypeSuggestionCandidate? SelectedCandidate,
    IReadOnlyCollection<CatalogProductTypeSuggestionCandidate> Candidates)
{
    public bool IsSuggested =>
        Status == CatalogProductTypeSuggestionStatus.Suggested;

    public bool IsConflict =>
        Status == CatalogProductTypeSuggestionStatus.Conflict;
}

public sealed record CatalogProductTypeSuggestionCandidate(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int HighestPriority,
    decimal Confidence,
    IReadOnlyCollection<CatalogProductTypeSuggestionEvidence> Evidence);

public sealed record CatalogProductTypeSuggestionEvidence(
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