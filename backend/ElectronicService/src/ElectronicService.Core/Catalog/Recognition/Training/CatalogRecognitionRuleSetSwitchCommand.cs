namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetSwitchCommand(
    Guid ManufacturerId,
    Guid ProductTypeId,
    long ExpectedSequenceNumber,
    Guid? NewVersionId,
    Guid? ReportId,
    string Reason,
    bool Confirmed);

public sealed record CatalogRecognitionRuleSetSwitchResult(
    Guid SwitchId,
    long SequenceNumber,
    Guid? PreviousVersionId,
    Guid? ActiveVersionId,
    DateTime CreatedAtUtc);