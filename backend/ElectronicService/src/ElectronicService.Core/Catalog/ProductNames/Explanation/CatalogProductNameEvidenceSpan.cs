namespace ElectronicService.Core.Catalog.ProductNames.Explanation;

public sealed record CatalogProductNameEvidenceSpan(
    CatalogProductNameEvidenceKind Kind,
    string TargetCode,
    string TargetValue,
    string RawValue,
    string Source,
    decimal Confidence,
    int Priority,
    int StartIndex,
    int Length)
{
    public int EndIndex => StartIndex + Length;
}