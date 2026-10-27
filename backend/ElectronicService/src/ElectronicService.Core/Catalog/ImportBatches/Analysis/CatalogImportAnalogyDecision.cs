namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public enum CatalogImportAnalogyDecisionKind
{
    None = 0,
    Accepted = 1,
    Rejected = 2
}

public sealed record CatalogImportAnalogyDecision(
    Guid CharacteristicDefinitionId,
    CatalogImportAnalogyDecisionKind Kind,
    string? Reason,
    Guid? FeedbackId = null);
