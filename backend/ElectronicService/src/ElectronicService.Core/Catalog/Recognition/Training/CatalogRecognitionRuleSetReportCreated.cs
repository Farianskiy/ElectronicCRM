namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetReportCreated(
    Guid Id,
    Guid RuleSetVersionId,
    Guid BatchId,
    DateTime CompletedAtUtc,
    int TotalRowsCount,
    int ProposedRowsCount,
    int ConflictRowsCount,
    int NoMatchRowsCount,
    int OutsideScopeRowsCount);