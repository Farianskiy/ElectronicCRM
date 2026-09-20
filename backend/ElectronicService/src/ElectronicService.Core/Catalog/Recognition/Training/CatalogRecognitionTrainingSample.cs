namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionTrainingScope(
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId);

public sealed record CatalogRecognitionTrainingSample(
    Guid ExampleId,
    Guid SourceFeedbackId,
    string ProductName,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    DateTime ConfirmedAtUtc);

public sealed record CatalogRecognitionTrainingSampleSet(
    CatalogRecognitionTrainingScope Scope,
    IReadOnlyList<CatalogRecognitionTrainingSample> Samples,
    bool HasMore);