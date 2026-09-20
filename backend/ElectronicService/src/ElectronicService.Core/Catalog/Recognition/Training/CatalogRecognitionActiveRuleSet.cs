namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetState(
    Guid ManufacturerId,
    Guid ProductTypeId,
    long SequenceNumber,
    Guid? SwitchId,
    Guid? ActiveVersionId,
    Guid? ReportId,
    DateTime? ChangedAtUtc);

public sealed record CatalogRecognitionActiveRuleSet(
    CatalogRecognitionRuleSetState State,
    CatalogRecognitionRuleSetExecutionSnapshot? Snapshot);