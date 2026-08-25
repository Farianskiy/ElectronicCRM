using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Datasets;

internal static class CatalogRecognitionDatasetJsonLineSerializer
{
    public const string CurrentFormatVersion = "1.0";

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static byte[] Serialize(CatalogRecognitionDatasetRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var jsonLine = new CatalogRecognitionDatasetJsonLine(
            CurrentFormatVersion,
            record.FeedbackId,
            record.ProductName,
            record.NormalizedProductName,
            record.ProductTypeId,
            record.ProductTypeCode,
            record.CharacteristicDefinitionId,
            record.CharacteristicCode,
            record.ExampleKind,
            record.FeedbackType,
            record.LabelQuality,
            record.SuggestedRawValue,
            record.SuggestedNormalizedValue,
            record.SuggestedConfidence,
            record.SuggestedSource,
            record.SpanStart,
            record.SpanLength,
            record.SpanEnd,
            record.FinalNormalizedValue,
            record.HasSuggestedSpan,
            record.HasVerifiedAnswerSpan,
            record.DictionaryTermId,
            record.RecognitionProfileId,
            record.ModelVersion,
            record.ImportBatchId,
            record.ImportRowId,
            record.ReviewerRole,
            record.CreatedAtUtc,
            record.FinalizedAtUtc);

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(jsonLine, SerializerOptions);
        var lineBytes = new byte[jsonBytes.Length + 1];

        jsonBytes.CopyTo(lineBytes, 0);
        lineBytes[^1] = (byte)'\n';

        return lineBytes;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        options.Converters.Add(new JsonStringEnumConverter<CatalogRecognitionDatasetExampleKind>());
        options.Converters.Add(new JsonStringEnumConverter<CatalogRecognitionFeedbackType>());
        options.Converters.Add(new JsonStringEnumConverter<CatalogRecognitionLabelQuality>());

        return options;
    }

    private sealed record CatalogRecognitionDatasetJsonLine(
        string FormatVersion,
        Guid FeedbackId,
        string ProductName,
        string NormalizedProductName,
        Guid ProductTypeId,
        string ProductTypeCode,
        Guid CharacteristicDefinitionId,
        string CharacteristicCode,
        CatalogRecognitionDatasetExampleKind ExampleKind,
        CatalogRecognitionFeedbackType FeedbackType,
        CatalogRecognitionLabelQuality LabelQuality,
        string? SuggestedRawValue,
        string? SuggestedNormalizedValue,
        decimal? SuggestedConfidence,
        string? SuggestedSource,
        int? SpanStart,
        int? SpanLength,
        int? SpanEnd,
        string? FinalNormalizedValue,
        bool HasSuggestedSpan,
        bool HasVerifiedAnswerSpan,
        Guid? DictionaryTermId,
        Guid? RecognitionProfileId,
        string? ModelVersion,
        Guid? ImportBatchId,
        Guid? ImportRowId,
        string? ReviewerRole,
        DateTime CreatedAtUtc,
        DateTime FinalizedAtUtc);
}