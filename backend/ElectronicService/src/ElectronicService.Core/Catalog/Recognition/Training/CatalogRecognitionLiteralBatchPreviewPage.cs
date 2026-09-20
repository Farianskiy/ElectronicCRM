namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionLiteralBatchPreviewPage(
    Guid DraftId,
    Guid BatchId,
    DateTime CheckedAtUtc,
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<CatalogRecognitionLiteralRowPreviewResult> Items);