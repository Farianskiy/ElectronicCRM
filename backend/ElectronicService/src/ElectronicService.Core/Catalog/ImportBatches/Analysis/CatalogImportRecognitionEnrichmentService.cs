using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Characteristics.Normalization;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportRecognitionEnrichmentService : ICatalogImportRecognitionEnrichmentService
{
    public const decimal MinimumAutomaticFillConfidence = 0.9800m;
    public const int MaximumAppliedValueDetails = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICatalogProductNameRecognitionService _recognitionService;
    private readonly ICatalogImportRowValidator _rowValidator;

    public CatalogImportRecognitionEnrichmentService(ICatalogProductNameRecognitionService recognitionService, ICatalogImportRowValidator rowValidator)
    {
        _recognitionService = recognitionService;
        _rowValidator = rowValidator;
    }

    public async Task<Result<CatalogImportRecognitionEnrichmentResult, DomainError>> EnrichAsync(
        CatalogImportWorkbookAnalysis analysis,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(characteristicDefinitions);

        if (analysis.MappingRequired)
        {
            return Result.Success<CatalogImportRecognitionEnrichmentResult, DomainError>(
                new CatalogImportRecognitionEnrichmentResult(
                    analysis,
                    CatalogImportRecognitionEnrichmentSummary.Empty));
        }

        var definitionsByCode = characteristicDefinitions
            .Where(definition => productType.AllowsCharacteristic(definition.Id))
            .GroupBy(
                definition => CatalogRecognitionTextNormalizer.NormalizeCode(definition.Code),
                StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single(),
                StringComparer.Ordinal);

        var allowedCharacteristicCodes = definitionsByCode.Keys.ToArray();

        var rowsAnalyzedCount = 0;
        var filledRowsCount = 0;
        var filledValuesCount = 0;
        var blockedByRecognitionConflictCount = 0;
        var blockedByLowConfidenceCount = 0;
        var blockedByUnsupportedSourceCount = 0;
        var blockedByInvalidExcelValueCount = 0;
        var blockedByInvalidRecognizedValueCount = 0;
        var failedRecognitionRowsCount = 0;

        var appliedValues = new List<CatalogImportRecognitionAppliedValue>(MaximumAppliedValueDetails);

        foreach (var row in analysis.Rows.OrderBy(row => row.RowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = DeserializeNormalizedData(row.NormalizedDataJson);
            var existingIssues = DeserializeRowIssues(row.IssuesJson);
            var existingWarnings = DeserializeRowIssues(row.WarningsJson);

            if (data is null || existingIssues is null || existingWarnings is null)
            {
                return Result.Failure<CatalogImportRecognitionEnrichmentResult, DomainError>(
                    CatalogImportErrors.InvalidNormalizedRow(row.RowNumber));
            }

            if (string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            rowsAnalyzedCount++;

            CatalogProductNameRecognitionResult recognitionResult;

            try
            {
                recognitionResult = await _recognitionService
                    .RecognizeAsync(
                        new CatalogProductNameRecognitionRequest(
                            data.Name,
                            productType.Id,
                            allowedCharacteristicCodes,
                            data.ManufacturerId),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (RegexMatchTimeoutException)
            {
                failedRecognitionRowsCount++;

                continue;
            }

            var characteristics = new Dictionary<string, string>(
                data.Characteristics,
                StringComparer.Ordinal);

            var characteristicOrigins = data.CharacteristicOrigins is null
                ? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal)
                : data.CharacteristicOrigins.ToDictionary(
                    item => item.Key,
                    item => item.Value,
                    StringComparer.Ordinal);

            var characteristicRecognitionSuggestions = new Dictionary<string, CatalogImportCharacteristicRecognitionSuggestion>(StringComparer.Ordinal);

            var conflictCodes = recognitionResult.Conflicts
                .Select(conflict => CatalogRecognitionTextNormalizer.NormalizeCode(conflict.CharacteristicCode))
                .ToHashSet(StringComparer.Ordinal);

            var recognitionIssues = new List<CatalogImportRowIssue>();
            var recognitionWarnings = new List<CatalogImportRowIssue>();

            foreach (var conflictCode in conflictCodes)
            {
                if (!definitionsByCode.TryGetValue(conflictCode, out var definition))
                {
                    continue;
                }

                var definitionKey = definition.Id.ToString();

                if (HasCharacteristicValue(characteristics, definitionKey))
                {
                    continue;
                }

                if (HasInvalidExplicitExcelValue(existingIssues, definitionKey))
                {
                    blockedByInvalidExcelValueCount++;

                    continue;
                }

                blockedByRecognitionConflictCount++;

                recognitionIssues.Add(
                    new CatalogImportRowIssue(
                        "characteristic.recognition_conflict",
                        $"В наименовании найдено несколько значений характеристики '{definition.Name}'. Укажите значение вручную.",
                        definitionKey,
                        null));
            }

            var appliedValuesInCurrentRow = new List<CatalogImportRecognitionAppliedValue>();

            foreach (var recognizedCharacteristic in recognitionResult.Characteristics)
            {
                var characteristicCode = CatalogRecognitionTextNormalizer.NormalizeCode(
                    recognizedCharacteristic.CharacteristicCode);

                if (!definitionsByCode.TryGetValue(characteristicCode, out var definition))
                {
                    continue;
                }

                var definitionKey = definition.Id.ToString();

                if (HasCharacteristicValue(characteristics, definitionKey))
                {
                    if (recognizedCharacteristic.Confidence >= MinimumAutomaticFillConfidence
                        && IsAutomaticFillSourceAllowed(recognizedCharacteristic)
                        && TryNormalizeRecognizedValue(
                            definition,
                            recognizedCharacteristic.NormalizedValue,
                            data.Manufacturer,
                            out var recognizedValue))
                    {
                        characteristicRecognitionSuggestions[definitionKey] = CatalogImportCharacteristicRecognitionSuggestion.FromRecognition(recognizedCharacteristic, recognizedValue);

                        var explicitValue = characteristics[definitionKey];

                        if (TryNormalizeRecognizedValue(
                                definition,
                                explicitValue,
                                data.Manufacturer,
                                out var normalizedExplicitValue))
                        {
                            explicitValue = normalizedExplicitValue;
                            characteristics[definitionKey] = normalizedExplicitValue;
                        }

                        if (!string.Equals(
                                explicitValue,
                                recognizedValue,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            recognitionIssues.Add(
                                new CatalogImportRowIssue(
                                    "characteristic.value_conflict",
                                    $"Значение характеристики '{definition.Name}' в Excel ('{explicitValue}') не совпадает со значением из наименования ('{recognizedValue}').",
                                    definitionKey,
                                    null));
                        }
                    }

                    continue;
                }

                if (HasInvalidExplicitExcelValue(existingIssues, definitionKey))
                {
                    blockedByInvalidExcelValueCount++;

                    continue;
                }

                if (conflictCodes.Contains(characteristicCode))
                {
                    continue;
                }

                if (recognizedCharacteristic.Confidence < MinimumAutomaticFillConfidence)
                {
                    blockedByLowConfidenceCount++;

                    recognitionWarnings.Add(
                        new CatalogImportRowIssue(
                            "characteristic.low_confidence",
                            $"Характеристика '{definition.Name}' распознана с недостаточной уверенностью ({recognizedCharacteristic.Confidence:P0}) и не заполнена автоматически.",
                            definitionKey,
                            null));

                    continue;
                }

                if (!IsAutomaticFillSourceAllowed(recognizedCharacteristic))
                {
                    blockedByUnsupportedSourceCount++;

                    continue;
                }

                if (!TryNormalizeRecognizedValue(
                        definition,
                        recognizedCharacteristic.NormalizedValue,
                        data.Manufacturer,
                        out var normalizedValue))
                {
                    blockedByInvalidRecognizedValueCount++;

                    continue;
                }

                characteristics[definitionKey] = normalizedValue;

                characteristicOrigins[definitionKey] = CatalogImportCharacteristicValueOrigin.FromRecognition(recognizedCharacteristic);

                appliedValuesInCurrentRow.Add(
                    new CatalogImportRecognitionAppliedValue(
                        row.RowNumber,
                        definition.Id,
                        definition.Code,
                        definition.Name,
                        normalizedValue,
                        recognizedCharacteristic.RawValue,
                        recognizedCharacteristic.Source,
                        recognizedCharacteristic.Confidence,
                        recognizedCharacteristic.StartIndex,
                        recognizedCharacteristic.Length,
                        recognizedCharacteristic.Priority,
                        recognizedCharacteristic.RecognizerKey));
            }

            var enrichedData = data with
            {
                Characteristics = characteristics,
                CharacteristicOrigins = characteristicOrigins,
                CharacteristicRecognitionSuggestions = characteristicRecognitionSuggestions
            };

            var validationResult = _rowValidator.Validate(
                enrichedData,
                productType,
                characteristicDefinitions);

            var combinedIssues = validationResult.Issues
                .Concat(
                    existingIssues.Where(issue =>
                        string.Equals(
                            issue.Code,
                            "characteristic.invalid",
                            StringComparison.Ordinal)))
                .Concat(recognitionIssues)
                .Distinct()
                .ToArray();

            var combinedWarnings = existingWarnings
                .Concat(validationResult.Warnings)
                .Concat(recognitionWarnings)
                .Distinct()
                .ToArray();

            var normalizedDataJson = JsonSerializer.Serialize(
                validationResult.Data,
                JsonOptions);

            var issuesJson = JsonSerializer.Serialize(
                combinedIssues,
                JsonOptions);

            var warningsJson = JsonSerializer.Serialize(
                combinedWarnings,
                JsonOptions);

            var replaceResult = row.ReplaceValidationResult(
                combinedIssues.Length == 0
                    ? CatalogImportRowStatus.Valid
                    : CatalogImportRowStatus.Error,
                normalizedDataJson,
                issuesJson,
                warningsJson);

            if (replaceResult.IsFailure)
            {
                return Result.Failure<CatalogImportRecognitionEnrichmentResult, DomainError>(
                    replaceResult.Error);
            }

            if (appliedValuesInCurrentRow.Count > 0)
            {
                filledRowsCount++;
                filledValuesCount += appliedValuesInCurrentRow.Count;
            }

            var remainingAppliedValueDetailsCapacity = MaximumAppliedValueDetails - appliedValues.Count;

            if (remainingAppliedValueDetailsCapacity > 0)
            {
                appliedValues.AddRange(appliedValuesInCurrentRow.Take(remainingAppliedValueDetailsCapacity));
            }
        }

        var validRowsCount = analysis.Rows.Count(
            row => row.Status == CatalogImportRowStatus.Valid);

        var errorRowsCount = analysis.Rows.Count(
            row => row.Status == CatalogImportRowStatus.Error);

        var enrichedAnalysis = analysis with
        {
            ValidRowsCount = validRowsCount,
            ErrorRowsCount = errorRowsCount
        };

        var summary = new CatalogImportRecognitionEnrichmentSummary(
            rowsAnalyzedCount,
            filledRowsCount,
            filledValuesCount,
            blockedByRecognitionConflictCount,
            blockedByLowConfidenceCount,
            blockedByUnsupportedSourceCount,
            blockedByInvalidExcelValueCount,
            blockedByInvalidRecognizedValueCount,
            failedRecognitionRowsCount,
            filledValuesCount > appliedValues.Count,
            appliedValues.ToArray());

        return Result.Success<CatalogImportRecognitionEnrichmentResult, DomainError>(
            new CatalogImportRecognitionEnrichmentResult(
                enrichedAnalysis,
                summary));
    }

    private static bool HasCharacteristicValue(Dictionary<string, string> characteristics, string definitionKey)
    {
        return characteristics.TryGetValue(definitionKey, out var value) &&
               !string.IsNullOrWhiteSpace(value);
    }

    private static bool HasInvalidExplicitExcelValue(CatalogImportRowIssue[] issues, string definitionKey)
    {
        return issues.Any(issue =>
            string.Equals(
                issue.Code,
                "characteristic.invalid",
                StringComparison.Ordinal) &&
            string.Equals(
                issue.Field,
                definitionKey,
                StringComparison.Ordinal));
    }

    private static bool IsAutomaticFillSourceAllowed(CatalogRecognizedCharacteristic characteristic)
    {
        if (characteristic.Source == CatalogRecognitionSource.Dictionary)
        {
            return true;
        }

        if (characteristic.Source != CatalogRecognitionSource.Rule)
        {
            return false;
        }

        if (characteristic.RecognizerKey.Contains(
                ":profile:",
                StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(
                   characteristic.RecognizerKey,
                   "rule:pole-count",
                   StringComparison.Ordinal) ||
               string.Equals(
                   characteristic.RecognizerKey,
                   "rule:ip-rating",
                   StringComparison.Ordinal);
    }

    private static bool TryNormalizeRecognizedValue(
    CharacteristicDefinition definition,
    string rawValue,
    string? manufacturerName,
    out string normalizedValue)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Text:
                return CatalogCharacteristicTextValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    manufacturerName,
                    out normalizedValue);

            case CharacteristicDataType.Number:
                return CatalogCharacteristicNumericValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    out normalizedValue);

            case CharacteristicDataType.Boolean:
                return CatalogCharacteristicBooleanValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    out normalizedValue);

            default:
                normalizedValue = string.Empty;

                return false;
        }
    }

    private static CatalogImportNormalizedRowData? DeserializeNormalizedData(string normalizedDataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(
                normalizedDataJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static CatalogImportRowIssue[]? DeserializeRowIssues(string issuesJson)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportRowIssue[]>(
                issuesJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
