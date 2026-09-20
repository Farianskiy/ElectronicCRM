namespace ElectronicService.Domain.Catalog.Recognition;

public sealed record CatalogRecognitionRuleSetSwitchData(
    Guid ManufacturerId,
    Guid ProductTypeId,
    long SequenceNumber,
    Guid? PreviousVersionId,
    Guid? NewVersionId,
    Guid? ReportId,
    Guid CreatedByUserId,
    string Reason);