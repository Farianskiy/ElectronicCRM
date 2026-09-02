namespace ElectronicService.Core.Catalog.ProductNames.Explanation;

public sealed record CatalogProductNameExplanationResult(
    string ProductName,
    IReadOnlyCollection<CatalogProductNameEvidenceSpan> Evidence,
    IReadOnlyCollection<CatalogProductNameUnexplainedSpan> UnexplainedSpans,
    int MeaningfulCharactersCount,
    int CoveredMeaningfulCharactersCount,
    decimal Coverage)
{
    public bool IsFullyExplained =>
        MeaningfulCharactersCount > 0 &&
        CoveredMeaningfulCharactersCount == MeaningfulCharactersCount;

    public bool HasUnexplainedSpans =>
        UnexplainedSpans.Count > 0;
}

public sealed record CatalogProductNameUnexplainedSpan(
    string RawValue,
    int StartIndex,
    int Length)
{
    public int EndIndex => StartIndex + Length;
}