namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerDraftEvidenceItem(
    Guid ExampleId,
    Guid CharacteristicDefinitionId,
    string ProductName,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    bool IsSupporting,
    DateTime ConfirmedAtUtc,
    DateTime? RevokedAtUtc);

public sealed record CatalogRecognitionMultiIntegerDraftEvidencePage(
    Guid DraftId,
    IReadOnlyList<CatalogRecognitionMultiIntegerDraftEvidenceItem> Items,
    int Page,
    int PageSize,
    bool HasMore);