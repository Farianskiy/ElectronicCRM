namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerAnnotation(
    Guid CharacteristicDefinitionId,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    int TokenIndex,
    IReadOnlyList<Guid> ExampleIds);

public sealed record CatalogRecognitionMultiIntegerSample(
    string ProductName,
    IReadOnlyList<CatalogRecognitionNameToken> Tokens,
    IReadOnlyList<CatalogRecognitionMultiIntegerAnnotation> Annotations);

public sealed record CatalogRecognitionMultiIntegerSampleSet(
    Guid ManufacturerId,
    Guid ProductTypeId,
    IReadOnlyList<Guid> CharacteristicDefinitionIds,
    IReadOnlyList<CatalogRecognitionMultiIntegerSample> Samples,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues)
{
    public bool CanGenerate => Samples.Count >= 2 && Issues.Count == 0;
}