namespace ElectronicService.Core.Catalog.Recognition.ConfirmTrainingExample;

public sealed record ConfirmCatalogRecognitionTrainingExampleCommand(
    Guid BatchId,
    Guid RowId,
    Guid CharacteristicDefinitionId,
    string ProductName,
    Guid ManufacturerId,
    Guid ProductTypeId,
    string NormalizedValue,
    string RawValue,
    int SpanStart,
    int SpanLength);