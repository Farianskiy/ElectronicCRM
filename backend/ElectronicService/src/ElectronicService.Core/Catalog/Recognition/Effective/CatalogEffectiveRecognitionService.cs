using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;
using static ElectronicService.Core.Catalog.Recognition.Effective.CatalogRecognitionCharacteristicValueNormalizer;

namespace ElectronicService.Core.Catalog.Recognition.Effective;

public sealed class CatalogEffectiveRecognitionService(
    ICatalogProductNameRecognitionService recognitionService,
    ICatalogRecognitionActiveRuleSetReader activeRuleSetReader,
    ICatalogRecognitionRuleSetExecutionReader executionReader) : ICatalogEffectiveRecognitionService
{
    public async Task<CatalogRecognitionRunContext> CreateRunAsync(CancellationToken cancellationToken = default)
    {
        var states = await activeRuleSetReader.CaptureForRunAsync(cancellationToken).ConfigureAwait(false);
        return new CatalogRecognitionRunContext(states);
    }

    public async Task<Result<CatalogEffectiveRecognitionResult, DomainError>> RecognizeAsync(
        CatalogEffectiveRecognitionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return new DomainError("recognition.name_required", "Укажите наименование товара.");
        }

        var definitionsByCode = (request.AllowedCharacteristics ?? [])
            .GroupBy(definition => CatalogRecognitionTextNormalizer.NormalizeCode(definition.Code), StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        try
        {
            var allowedCodes = request.AllowedCharacteristics is null ? null : definitionsByCode.Keys.ToArray();
            var baseline = request.AllowedCharacteristics is not null && definitionsByCode.Count == 0
                ? new CatalogProductNameRecognitionResult(request.ProductName, CatalogRecognitionTextNormalizer.NormalizeText(request.ProductName), [], [], [])
                : await recognitionService.RecognizeAsync(new CatalogProductNameRecognitionRequest(
                    request.ProductName, request.ProductTypeId,
                    allowedCodes,
                    request.ManufacturerId, request.Context), cancellationToken).ConfigureAwait(false);
            var merged = await MergeActiveRulesAsync(request, definitionsByCode, baseline, cancellationToken).ConfigureAwait(false);
            if (merged.IsFailure)
            {
                return merged.Error;
            }

            var completeScope = request.ManufacturerId.HasValue && request.ManufacturerId != Guid.Empty
                && request.ProductTypeId.HasValue && request.ProductTypeId != Guid.Empty;
            CatalogRecognitionRuleSetState? state = null;
            if (completeScope)
            {
                state = request.Context.ActiveRuleSets[(request.ManufacturerId!.Value, request.ProductTypeId!.Value)].Value.State;
            }

            var profiles = request.ProductTypeId.HasValue && request.Context.Profiles.TryGetValue(request.ProductTypeId.Value, out var cached)
                ? cached : [];
            return new CatalogEffectiveRecognitionResult(merged.Value, completeScope, state, profiles);
        }
        catch (RegexMatchTimeoutException)
        {
            return new DomainError("recognition.timeout", "Распознавание превысило допустимое время. Проверьте шаблоны распознавания.");
        }
    }

    private async Task<Result<CatalogRecognitionActiveRuleSet, DomainError>> LoadActiveAsync(
        CatalogEffectiveRecognitionRequest request, CancellationToken cancellationToken)
    {
        var key = (request.ManufacturerId!.Value, request.ProductTypeId!.Value);
        if (request.Context.ActiveRuleSets.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var state = request.Context.ActiveStates.GetValueOrDefault(key)
            ?? new CatalogRecognitionRuleSetState(key.Item1, key.Item2, 0, null, null, null, null);
        Result<CatalogRecognitionActiveRuleSet, DomainError> result;
        if (state.ActiveVersionId is not Guid versionId)
        {
            result = new CatalogRecognitionActiveRuleSet(state, null);
        }
        else
        {
            var loaded = await executionReader.ReadAsync(versionId, cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
            {
                result = new DomainError("recognition.active_rule_invalid", loaded.Error.Message);
            }
            else if (loaded.Value.VersionId != versionId || loaded.Value.ManufacturerId != key.Item1 || loaded.Value.ProductTypeId != key.Item2)
            {
                result = new DomainError("recognition.scope_mismatch", "Активная версия не соответствует области распознавания.");
            }
            else
            {
                result = new CatalogRecognitionActiveRuleSet(state, loaded.Value);
            }
        }

        request.Context.ActiveRuleSets.Add(key, result);
        return result;
    }

    private async Task<
    Result<CatalogProductNameRecognitionResult, DomainError>>
    MergeActiveRulesAsync(
        CatalogEffectiveRecognitionRequest request,
        Dictionary<string, CharacteristicDefinition> definitionsByCode,
        CatalogProductNameRecognitionResult recognition,
        CancellationToken cancellationToken)
    {
        if (request.ManufacturerId is not Guid manufacturerId ||
            manufacturerId == Guid.Empty || !request.ProductTypeId.HasValue)
        {
            return recognition;
        }

        var loaded = await LoadActiveAsync(request, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var active = loaded.Value;
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
            request.ProductTypeId!.Value,
            recognition.ProductName,
            cancellationToken);

        if (executed.IsFailure)
        {
            return new DomainError("recognition.active_rule_invalid", executed.Error.Message);
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
                    request.ManufacturerName,
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
            request.ManufacturerName);
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

}
