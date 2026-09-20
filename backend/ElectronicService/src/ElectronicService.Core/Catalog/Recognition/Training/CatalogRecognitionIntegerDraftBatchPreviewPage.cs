namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerDraftBatchPreviewPage(
    Guid DraftId,
    Guid BatchId,
    CatalogRecognitionTrainingScope Scope,
    CatalogRecognitionIntegerAlternativesPattern Pattern,
    DateTime CheckedAtUtc,
    int Page,
    int PageSize,
    bool HasMore,
    CatalogRecognitionIntegerDraftRecheckResult Recheck,
    IReadOnlyList<CatalogRecognitionIntegerRowPreviewResult> Items);