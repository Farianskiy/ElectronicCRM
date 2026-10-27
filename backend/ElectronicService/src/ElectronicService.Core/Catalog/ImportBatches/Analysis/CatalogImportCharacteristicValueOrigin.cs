using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportCharacteristicValueOrigin(
    CatalogImportCharacteristicValueSource Source,
    int? SourceColumnNumber,
    string? RecognitionSource,
    decimal? Confidence,
    string? RawValue,
    int? SpanStart,
    int? SpanLength,
    int? Priority,
    string? RecognizerKey,
    Guid? AnalogyConfigurationId = null,
    int? AnalogyConfigurationVersion = null,
    IReadOnlyCollection<Guid>? AnalogySupportingProductIds = null,
    string? AnalogyConfigurationHash = null,
    string? AnalogyCandidateSnapshotId = null,
    DateTime? AnalogyCandidateSnapshotCreatedAtUtc = null,
    string? AnalogyFamilyAnchor = null,
    int? AnalogySupportingProductCount = null,
    IReadOnlyCollection<Guid>? TrainingExampleIds = null,
    IReadOnlyCollection<Guid>? TrainingSourceRowIds = null)
{
    public static CatalogImportCharacteristicValueOrigin FromExcel(int sourceColumnNumber)
    {
        return new CatalogImportCharacteristicValueOrigin(
            CatalogImportCharacteristicValueSource.Excel,
            sourceColumnNumber,
            RecognitionSource: null,
            Confidence: null,
            RawValue: null,
            SpanStart: null,
            SpanLength: null,
            Priority: null,
            RecognizerKey: null);
    }

    public static CatalogImportCharacteristicValueOrigin FromRecognition(CatalogRecognizedCharacteristic characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);

        return new CatalogImportCharacteristicValueOrigin(
            CatalogImportCharacteristicValueSource.Recognition,
            SourceColumnNumber: null,
            characteristic.Source.ToString(),
            characteristic.Confidence,
            characteristic.RawValue,
            characteristic.StartIndex,
            characteristic.Length,
            characteristic.Priority,
            characteristic.RecognizerKey);
    }

    public static CatalogImportCharacteristicValueOrigin FromManual()
    {
        return new CatalogImportCharacteristicValueOrigin(
            CatalogImportCharacteristicValueSource.Manual,
            SourceColumnNumber: null,
            RecognitionSource: null,
            Confidence: null,
            RawValue: null,
            SpanStart: null,
            SpanLength: null,
            Priority: null,
            RecognizerKey: null);
    }

    public static CatalogImportCharacteristicValueOrigin FromCatalogAnalogy(
        string proposedValue,
        decimal supportRatio,
        Guid? configurationId = null,
        int? configurationVersion = null,
        IReadOnlyCollection<Guid>? supportingProductIds = null,
        string? configurationHash = null,
        string? candidateSnapshotId = null,
        DateTime? candidateSnapshotCreatedAtUtc = null,
        string? familyAnchor = null,
        int? supportingProductCount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proposedValue);

        return new CatalogImportCharacteristicValueOrigin(
            CatalogImportCharacteristicValueSource.CatalogAnalogy,
            SourceColumnNumber: null,
            RecognitionSource: "CatalogAnalogy",
            Confidence: supportRatio,
            RawValue: proposedValue.Trim(),
            SpanStart: null,
            SpanLength: null,
            Priority: null,
            RecognizerKey: "catalog-analogy:v3",
            configurationId,
            configurationVersion,
            supportingProductIds,
            configurationHash,
            candidateSnapshotId,
            candidateSnapshotCreatedAtUtc,
            familyAnchor,
            supportingProductCount);
    }

    public static CatalogImportCharacteristicValueOrigin FromTrainingExamples(
        string rawValue,
        decimal confidence,
        IReadOnlyCollection<Guid> exampleIds,
        IReadOnlyCollection<Guid> sourceRowIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);
        ArgumentNullException.ThrowIfNull(exampleIds);
        ArgumentNullException.ThrowIfNull(sourceRowIds);

        return new CatalogImportCharacteristicValueOrigin(
            CatalogImportCharacteristicValueSource.TrainingExample,
            SourceColumnNumber: null,
            RecognitionSource: "TrainingExample",
            confidence,
            rawValue.Trim(),
            SpanStart: null,
            SpanLength: null,
            Priority: null,
            RecognizerKey: "confirmed-example-fill:v1",
            TrainingExampleIds: exampleIds.Distinct().ToArray(),
            TrainingSourceRowIds: sourceRowIds.Distinct().ToArray());
    }
}
