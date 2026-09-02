namespace ElectronicService.Core.Catalog.ProductNames.Explanation;

public interface ICatalogProductNameEvidenceCoverageService
{
    CatalogProductNameExplanationResult Explain(
        string productName,
        IReadOnlyCollection<CatalogProductNameEvidenceSpan> evidence);
}