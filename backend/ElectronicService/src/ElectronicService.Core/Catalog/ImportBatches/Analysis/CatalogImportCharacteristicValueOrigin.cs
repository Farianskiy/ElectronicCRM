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
    string? RecognizerKey)
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
}