using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportCharacteristicRecognitionSuggestion(
    string CharacteristicCode,
    string RawValue,
    string NormalizedValue,
    string RecognitionSource,
    decimal Confidence,
    int SpanStart,
    int SpanLength,
    int Priority,
    string RecognizerKey)
{
    public static CatalogImportCharacteristicRecognitionSuggestion FromRecognition(CatalogRecognizedCharacteristic characteristic, string normalizedValue)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedValue);

        return new CatalogImportCharacteristicRecognitionSuggestion(
            characteristic.CharacteristicCode,
            characteristic.RawValue,
            normalizedValue,
            characteristic.Source.ToString(),
            characteristic.Confidence,
            characteristic.StartIndex,
            characteristic.Length,
            characteristic.Priority,
            characteristic.RecognizerKey);
    }
}