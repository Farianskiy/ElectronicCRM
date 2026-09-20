namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionTrainingIssue(
    string Code,
    string Message,
    IReadOnlyList<Guid> ExampleIds);

public sealed record CatalogRecognitionPreparedSample(
    string ProductName,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    IReadOnlyList<Guid> ExampleIds);

public sealed record CatalogRecognitionPreparedSampleSet(
    CatalogRecognitionTrainingScope Scope,
    int SourceCount,
    int DuplicateCount,
    IReadOnlyList<CatalogRecognitionPreparedSample> Samples,
    IReadOnlyList<CatalogRecognitionTrainingIssue> Issues)
{
    public bool CanGenerate => Samples.Count > 0 && Issues.Count == 0;
}