namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerDraftEvidenceItem(
    Guid ExampleId,
    string ProductName,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    bool IsSupporting,
    DateTime ConfirmedAtUtc,
    DateTime? RevokedAtUtc);

public sealed record CatalogRecognitionIntegerDraftEvidencePage(
    Guid DraftId,
    IReadOnlyList<CatalogRecognitionIntegerDraftEvidenceItem> Items,
    int Page,
    int PageSize,
    bool HasMore);