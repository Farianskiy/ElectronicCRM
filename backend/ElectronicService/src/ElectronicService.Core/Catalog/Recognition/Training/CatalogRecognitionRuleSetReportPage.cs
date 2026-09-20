namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetReportPage(
    CatalogRecognitionRuleSetReportCreated Summary,
    DateTime StartedAtUtc,
    long BatchVersion,
    string EvaluatorVersion,
    int SnapshotFormatVersion,
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<CatalogRecognitionRuleSetBatchPreviewItem> Items);