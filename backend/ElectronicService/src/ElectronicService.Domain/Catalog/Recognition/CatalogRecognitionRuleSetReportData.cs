namespace ElectronicService.Domain.Catalog.Recognition;

public sealed record CatalogRecognitionRuleSetReportData(
    Guid RuleSetVersionId,
    Guid BatchId,
    uint BatchVersion,
    Guid CreatedByUserId,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    string EvaluatorVersion,
    int TotalRowsCount,
    int ProposedRowsCount,
    int ConflictRowsCount,
    int NoMatchRowsCount,
    int OutsideScopeRowsCount,
    string SnapshotJson);