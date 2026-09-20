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
using ElectronicService.Core.Catalog.Recognition.Training;

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
    private readonly ICatalogRecognitionActiveRuleSetReader _activeRuleSetReader;

    public CatalogImportRecognitionEnrichmentService(
        ICatalogProductNameRecognitionService recognitionService,
        ICatalogImportRowValidator rowValidator,
        ICatalogRecognitionActiveRuleSetReader activeRuleSetReader)
    {
        ArgumentNullException.ThrowIfNull(recognitionService);
        ArgumentNullException.ThrowIfNull(rowValidator);
        ArgumentNullException.ThrowIfNull(activeRuleSetReader);

        _recognitionService = recognitionService;
        _rowValidator = rowValidator;
        _activeRuleSetReader = activeRuleSetReader;
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

        var definitionsByCode = GetDefinitionsByCode(productType, characteristicDefinitions);

        var activeRuleSets =
            new Dictionary<Guid, CatalogRecognitionActiveRuleSet>();

        var rowsAnalyzedCount = 0;
        var filledRowsCount = 0;
        var filledValuesCount = 0;
        var blockedByRecognitionConflictCount = 0;
        var blockedByLowConfidenceCount = 0;
        var blockedByUnsupportedSourceCount = 0;
        var blockedByInvalidExcelValueCount = 0;
        var blockedByInvalidRecognizedValueCount = 0;
        const int failedRecognitionRowsCount = 0;

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
                var recognized = await RecognizeRowCoreAsync(
                        data,
                        productType,
                        definitionsByCode,
                        activeRuleSets,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (recognized.IsFailure)
                {
                    return recognized.Error;
                }

                recognitionResult = recognized.Value;
            }
            catch (RegexMatchTimeoutException)
            {
                return Result.Failure<
                    CatalogImportRecognitionEnrichmentResult,
                    DomainError>(
                    new DomainError(
                        "recognition.timeout",
                        $"Распознавание строки {row.RowNumber} превысило "
                        + "допустимое время. Анализ остановлен. "
                        + "Проверьте шаблоны распознавания для этого товара."));
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
                    recognitionWarnings.Add(
                        new CatalogImportRowIssue(
                            "characteristic.recognition_conflict",
                            $"Правила распознавания дали противоречащие "
                            + $"результаты для характеристики "
                            + $"'{definition.Name}'. Сохранённое значение "
                            + "оставлено без изменений. Проверьте правила.",
                            definitionKey,
                            null));

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

                if (conflictCodes.Contains(characteristicCode))
                {
                    continue;
                }

                if (characteristicOrigins.TryGetValue(definitionKey, out var origin)
                    && origin.Source == CatalogImportCharacteristicValueSource.Manual)
                {
                    continue;
                }

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

                if (recognizedCharacteristic.Confidence < MinimumAutomaticFillConfidence)
                {
                    blockedByLowConfidenceCount++;

                    if (TryNormalizeRecognizedValue(
                            definition,
                            recognizedCharacteristic.NormalizedValue,
                            data.Manufacturer,
                            out var suggestedValue))
                    {
                        characteristicRecognitionSuggestions[definitionKey] =
                            CatalogImportCharacteristicRecognitionSuggestion.FromRecognition(
                                recognizedCharacteristic, suggestedValue);
                    }

                    recognitionWarnings.Add(
                        new CatalogImportRowIssue(
                            "characteristic.low_confidence",
                            $"Характеристика '{definition.Name}': предложено '{recognizedCharacteristic.NormalizedValue}' с недостаточной уверенностью ({recognizedCharacteristic.Confidence:P0}); значение не заполнено автоматически.",
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
                .Where(warning => !string.Equals(warning.Code, "characteristic.low_confidence", StringComparison.Ordinal)
                    && !string.Equals(warning.Code, "characteristic.recognition_conflict", StringComparison.Ordinal))
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

        if (characteristic.RecognizerKey.StartsWith(
        "active-rule-set:",
        StringComparison.Ordinal))
        {
            return true;
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

    private async Task<
    Result<CatalogProductNameRecognitionResult, DomainError>>
    MergeActiveRulesAsync(
        CatalogImportNormalizedRowData data,
        ProductType productType,
        Dictionary<string, CharacteristicDefinition> definitionsByCode,
        CatalogProductNameRecognitionResult recognition,
        Dictionary<Guid, CatalogRecognitionActiveRuleSet> activeRuleSets,
        CancellationToken cancellationToken)
    {
        if (data.ManufacturerId is not Guid manufacturerId ||
            manufacturerId == Guid.Empty)
        {
            return recognition;
        }

        if (!activeRuleSets.TryGetValue(manufacturerId, out var active))
        {
            var loaded = await _activeRuleSetReader.LoadAsync(
                manufacturerId,
                productType.Id,
                cancellationToken)
                .ConfigureAwait(false);

            if (loaded.IsFailure)
            {
                return loaded.Error;
            }

            active = loaded.Value;
            activeRuleSets.Add(manufacturerId, active);
        }

        var snapshot = active.Snapshot;

        if (snapshot is null)
        {
            return recognition;
        }

        var definitionsById = definitionsByCode.Values
            .ToDictionary(definition => definition.Id);

        var numericIds = snapshot.NumericRules
            .Select(rule => rule.CharacteristicDefinitionId)
            .Concat(
                snapshot.MultiNumericRules.SelectMany(rule =>
                    rule.Pattern.Parts
                        .Where(part =>
                            part.CharacteristicDefinitionId.HasValue)
                        .Select(part =>
                            part.CharacteristicDefinitionId
                                .GetValueOrDefault())))
            .ToHashSet();

        var referencedIds = snapshot.LiteralRules
            .Select(rule => rule.CharacteristicDefinitionId)
            .Concat(numericIds)
            .Distinct();

        foreach (var characteristicId in referencedIds)
        {
            if (!definitionsById.TryGetValue(
                    characteristicId,
                    out var definition))
            {
                return new DomainError(
                    "recognition.active_rule_invalid",
                    $"Характеристика {characteristicId} активной версии "
                    + "отсутствует среди доступных характеристик типа товара.");
            }

            if (numericIds.Contains(characteristicId) &&
                definition.DataType != CharacteristicDataType.Number)
            {
                return new DomainError(
                    "recognition.active_rule_invalid",
                    $"Числовое правило активной версии ссылается "
                    + $"на нечисловую характеристику '{definition.Name}'.");
            }
        }

        var executed = CatalogRecognitionRuleSetRowExecutor.Execute(
            snapshot,
            manufacturerId,
            productType.Id,
            recognition.ProductName,
            cancellationToken);

        if (executed.IsFailure)
        {
            return executed.Error;
        }

        var activeCandidates =
            new List<CatalogRecognizedCharacteristic>();

        foreach (var candidate in executed.Value.Characteristics
                     .SelectMany(item => item.Sources))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var definition =
                definitionsById[candidate.CharacteristicDefinitionId];

            if (!TryNormalizeRecognizedValue(
                    definition,
                    candidate.NormalizedValue,
                    data.Manufacturer,
                    out var normalizedValue))
            {
                return new DomainError(
                    "recognition.active_rule_invalid",
                    $"Активная версия вернула недопустимое значение "
                    + $"характеристики '{definition.Name}'.");
            }

            activeCandidates.Add(new CatalogRecognizedCharacteristic(
                CatalogRecognitionTextNormalizer.NormalizeCode(
                    definition.Code),
                candidate.RawValue,
                normalizedValue,
                Confidence: 1.0000m,
                Source: CatalogRecognitionSource.Rule,
                StartIndex: candidate.SpanStart,
                Length: candidate.SpanLength,
                Priority: 0,
                RecognizerKey:
                    $"active-rule-set:{snapshot.VersionId}"
                    + $":draft:{candidate.DraftId}"));
        }

        return MergeRecognitionCandidates(
            recognition,
            activeCandidates,
            definitionsByCode,
            data.Manufacturer);
    }

    private static CatalogProductNameRecognitionResult
        MergeRecognitionCandidates(
            CatalogProductNameRecognitionResult recognition,
            List<CatalogRecognizedCharacteristic> activeCandidates,
            Dictionary<string, CharacteristicDefinition> definitionsByCode,
            string? manufacturerName)
    {
        if (activeCandidates.Count == 0)
        {
            return recognition;
        }

        var characteristics = recognition.Characteristics.ToList();
        var conflicts = recognition.Conflicts.ToList();

        var existingConflictCodes = recognition.Conflicts
            .Select(conflict =>
                CatalogRecognitionTextNormalizer.NormalizeCode(
                    conflict.CharacteristicCode))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var group in activeCandidates.GroupBy(
                     candidate => candidate.CharacteristicCode,
                     StringComparer.Ordinal))
        {
            var code = group.Key;
            var definition = definitionsByCode[code];

            var previous = characteristics
                .Where(candidate => string.Equals(
                    CatalogRecognitionTextNormalizer.NormalizeCode(
                        candidate.CharacteristicCode),
                    code,
                    StringComparison.Ordinal))
                .ToArray();

            characteristics.RemoveAll(candidate => string.Equals(
                CatalogRecognitionTextNormalizer.NormalizeCode(
                    candidate.CharacteristicCode),
                code,
                StringComparison.Ordinal));

            var combined = group.Concat(previous)
                .Select(candidate =>
                {
                    if (TryNormalizeRecognizedValue(
                            definition,
                            candidate.NormalizedValue,
                            manufacturerName,
                            out var normalizedValue))
                    {
                        return candidate with
                        {
                            NormalizedValue = normalizedValue
                        };
                    }

                    return candidate;
                })
                .ToArray();

            if (existingConflictCodes.Contains(code))
            {
                // Старый конфликт не снимается новым совпадением.
                continue;
            }

            var values = combined
                .Select(candidate => candidate.NormalizedValue)
                .Distinct(StringComparer.Ordinal)
                .Take(2)
                .ToArray();

            if (values.Length > 1)
            {
                conflicts.Add(new CatalogRecognitionConflict(
                    code,
                    combined));

                continue;
            }

            // Первым идёт результат активной версии.
            // Сохраняем его источник, а не источник старого распознавателя.
            characteristics.Add(combined[0]);
        }

        return recognition with
        {
            Characteristics = characteristics.ToArray(),
            Conflicts = conflicts.ToArray(),
            Candidates = recognition.Candidates
                .Concat(activeCandidates)
                .ToArray()
        };
    }

    public Task<Result<CatalogProductNameRecognitionResult, DomainError>> RecognizeRowAsync(
        CatalogImportNormalizedRowData data,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(characteristicDefinitions);

        return RecognizeRowCoreAsync(
            data,
            productType,
            GetDefinitionsByCode(productType, characteristicDefinitions),
            new Dictionary<Guid, CatalogRecognitionActiveRuleSet>(),
            cancellationToken);
    }

    private async Task<Result<CatalogProductNameRecognitionResult, DomainError>> RecognizeRowCoreAsync(
        CatalogImportNormalizedRowData data,
        ProductType productType,
        Dictionary<string, CharacteristicDefinition> definitionsByCode,
        Dictionary<Guid, CatalogRecognitionActiveRuleSet> activeRuleSets,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(data.Name))
        {
            return new DomainError(
                "recognition.name_required",
                "Укажите наименование товара.");
        }

        if (data.ProductTypeId.HasValue &&
            data.ProductTypeId.Value != productType.Id)
        {
            return new DomainError(
                "recognition.scope_mismatch",
                "Тип строки не совпадает с областью распознавания.");
        }

        var baseline = definitionsByCode.Count == 0
            ? new CatalogProductNameRecognitionResult(
                data.Name,
                CatalogRecognitionTextNormalizer.NormalizeText(data.Name),
                [],
                [],
                [])
            : await _recognitionService.RecognizeAsync(
                new CatalogProductNameRecognitionRequest(
                    data.Name,
                    productType.Id,
                    definitionsByCode.Keys.ToArray(),
                    data.ManufacturerId),
                cancellationToken).ConfigureAwait(false);

        return await MergeActiveRulesAsync(
            data,
            productType,
            definitionsByCode,
            baseline,
            activeRuleSets,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private static Dictionary<string, CharacteristicDefinition> GetDefinitionsByCode(
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions)
    {
        return characteristicDefinitions
            .Where(definition => productType.AllowsCharacteristic(definition.Id))
            .GroupBy(
                definition => CatalogRecognitionTextNormalizer.NormalizeCode(definition.Code),
                StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single(),
                StringComparer.Ordinal);
    }

    public Task PrepareRunAsync(
        CancellationToken cancellationToken = default)
    {
        return _activeRuleSetReader.CaptureForRunAsync(
            cancellationToken);
    }
}
