namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerBatchPreviewRequest(
    Guid BatchId,
    CatalogRecognitionTrainingScope Scope,
    string GeneratorVersion,
    CatalogRecognitionIntegerAlternativesPattern Pattern,
    int Page,
    int PageSize);

public sealed record CatalogRecognitionIntegerBatchPreviewItem(
    CatalogRecognitionIntegerRowPreviewResult Result,
    bool MatchesTrainingName);

public sealed record CatalogRecognitionIntegerBatchPreviewPage(
    Guid BatchId,
    CatalogRecognitionTrainingScope Scope,
    string GeneratorVersion,
    CatalogRecognitionIntegerAlternativesPattern Pattern,
    DateTime CheckedAtUtc,
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<CatalogRecognitionIntegerBatchPreviewItem> Items,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Diagnostics);