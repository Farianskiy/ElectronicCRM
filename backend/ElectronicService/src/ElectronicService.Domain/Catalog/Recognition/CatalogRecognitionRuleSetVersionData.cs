namespace ElectronicService.Domain.Catalog.Recognition;

public sealed record CatalogRecognitionRuleSetEntryData(
    CatalogRecognitionRuleKind Kind,
    Guid DraftId);

public sealed record CatalogRecognitionRuleSetVersionData(
    Guid ManufacturerId,
    Guid ProductTypeId,
    int VersionNumber,
    string Name,
    Guid CreatedByUserId,
    IReadOnlyList<CatalogRecognitionRuleSetEntryData> Entries);