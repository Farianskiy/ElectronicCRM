using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportRecognitionFeedbackCollector : ICatalogImportRecognitionFeedbackCollector
{
    private readonly ICatalogRecognitionFeedbackRepository _feedbackRepository;

    public CatalogImportRecognitionFeedbackCollector(ICatalogRecognitionFeedbackRepository feedbackRepository)
    {
        ArgumentNullException.ThrowIfNull(feedbackRepository);

        _feedbackRepository = feedbackRepository;
    }

    public async Task<Result<CatalogImportRecognitionFeedbackCollectionResult, DomainError>> CollectAsync(CatalogImportRecognitionFeedbackCollectionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ProductType);
        ArgumentNullException.ThrowIfNull(request.CharacteristicDefinitions);
        ArgumentNullException.ThrowIfNull(request.Before);
        ArgumentNullException.ThrowIfNull(request.After);

        if (request.ImportBatchId == Guid.Empty)
        {
            return Result.Failure<CatalogImportRecognitionFeedbackCollectionResult, DomainError>(GeneralErrors.ValueIsInvalid(nameof(request.ImportBatchId)));
        }

        if (request.ImportRowId == Guid.Empty)
        {
            return Result.Failure<CatalogImportRecognitionFeedbackCollectionResult, DomainError>(GeneralErrors.ValueIsInvalid(nameof(request.ImportRowId)));
        }

        var beforeValues = GetCharacteristicValuesByDefinitionId(request.Before.Characteristics);
        var afterValues = GetCharacteristicValuesByDefinitionId(request.After.Characteristics);
        var beforeOrigins = GetCharacteristicOriginsByDefinitionId(request.Before.CharacteristicOrigins);
        var mergedOrigins = BuildMergedOrigins(request.After.Characteristics, beforeValues, beforeOrigins);

        var dataWithPreservedOrigins = request.After with
        {
            CharacteristicOrigins = mergedOrigins
        };

        var existingFeedback = await _feedbackRepository.GetByImportRowAsync(request.ImportRowId, cancellationToken).ConfigureAwait(false);

        var existingFeedbackByDefinitionId = existingFeedback.ToDictionary(feedback => feedback.CharacteristicDefinitionId);

        var definitionsById = request.CharacteristicDefinitions.ToDictionary(definition => definition.Id);

        var characteristicDefinitionIds = new HashSet<Guid>(beforeValues.Keys);
        characteristicDefinitionIds.UnionWith(afterValues.Keys);
        characteristicDefinitionIds.UnionWith(existingFeedbackByDefinitionId.Keys);

        var createdFeedbackCount = 0;
        var updatedFeedbackCount = 0;
        var removedFeedbackCount = 0;

        foreach (var characteristicDefinitionId in characteristicDefinitionIds)
        {
            if (!definitionsById.TryGetValue(characteristicDefinitionId, out var characteristicDefinition))
            {
                continue;
            }

            var hasBeforeValue = beforeValues.TryGetValue(characteristicDefinitionId, out var beforeValue);
            var hasAfterValue = afterValues.TryGetValue(characteristicDefinitionId, out var afterValue);
            beforeOrigins.TryGetValue(characteristicDefinitionId, out var beforeOrigin);
            existingFeedbackByDefinitionId.TryGetValue(characteristicDefinitionId, out var currentFeedback);

            if (currentFeedback?.IsFinalized == true)
            {
                continue;
            }

            if (currentFeedback?.IsPending == true)
            {
                var pendingDecision = ClassifyExistingPendingFeedback(currentFeedback, hasAfterValue, afterValue);

                if (pendingDecision is null)
                {
                    _feedbackRepository.Remove(currentFeedback);
                    removedFeedbackCount++;
                    continue;
                }

                var updateResult = currentFeedback.UpdatePendingDecision(pendingDecision.FeedbackType, pendingDecision.FinalNormalizedValue);

                if (updateResult.IsFailure)
                {
                    return Result.Failure<CatalogImportRecognitionFeedbackCollectionResult, DomainError>(updateResult.Error);
                }

                updatedFeedbackCount++;
                continue;
            }

            var newDecision = ClassifyNewFeedback(hasBeforeValue, beforeValue, beforeOrigin, hasAfterValue, afterValue);

            if (newDecision is null)
            {
                continue;
            }

            var createResult = CreateFeedback(
                request,
                characteristicDefinition,
                beforeValue,
                beforeOrigin,
                newDecision);

            if (createResult.IsFailure)
            {
                return Result.Failure<CatalogImportRecognitionFeedbackCollectionResult, DomainError>(createResult.Error);
            }

            if (createResult.Value is null)
            {
                continue;
            }

            _feedbackRepository.Add(createResult.Value);
            createdFeedbackCount++;
        }

        return new CatalogImportRecognitionFeedbackCollectionResult(
            dataWithPreservedOrigins,
            createdFeedbackCount,
            updatedFeedbackCount,
            removedFeedbackCount);
    }

    public async Task<Result<int, DomainError>> RemovePendingForBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        if (importBatchId == Guid.Empty)
        {
            return Result.Failure<int, DomainError>(GeneralErrors.ValueIsInvalid(nameof(importBatchId)));
        }

        var pendingFeedback = await _feedbackRepository.GetPendingByImportBatchAsync(importBatchId, cancellationToken).ConfigureAwait(false);

        foreach (var feedback in pendingFeedback)
        {
            _feedbackRepository.Remove(feedback);
        }

        return pendingFeedback.Count;
    }

    private static FeedbackDecision? ClassifyExistingPendingFeedback(CatalogRecognitionFeedback feedback, bool hasAfterValue, string? afterValue)
    {
        if (feedback.SuggestedNormalizedValue is not null)
        {
            if (!hasAfterValue)
            {
                return new FeedbackDecision(CatalogRecognitionFeedbackType.Rejected, FinalNormalizedValue: null, HasRecognitionEvidence: true);
            }

            if (string.Equals(feedback.SuggestedNormalizedValue, afterValue, StringComparison.Ordinal))
            {
                return new FeedbackDecision(CatalogRecognitionFeedbackType.Accepted, afterValue, HasRecognitionEvidence: true);
            }

            return new FeedbackDecision(CatalogRecognitionFeedbackType.Corrected, afterValue, HasRecognitionEvidence: true);
        }

        if (!hasAfterValue)
        {
            return null;
        }

        return new FeedbackDecision(CatalogRecognitionFeedbackType.AddedManually, afterValue, HasRecognitionEvidence: false);
    }

    private static FeedbackDecision? ClassifyNewFeedback(
        bool hasBeforeValue,
        string? beforeValue,
        CatalogImportCharacteristicValueOrigin? beforeOrigin,
        bool hasAfterValue,
        string? afterValue)
    {
        if (hasBeforeValue && beforeOrigin?.Source == CatalogImportCharacteristicValueSource.Recognition)
        {
            if (!HasCompleteRecognitionEvidence(beforeOrigin))
            {
                return null;
            }

            if (!hasAfterValue)
            {
                return new FeedbackDecision(CatalogRecognitionFeedbackType.Rejected, FinalNormalizedValue: null, HasRecognitionEvidence: true);
            }

            if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
            {
                return new FeedbackDecision(CatalogRecognitionFeedbackType.Accepted, afterValue, HasRecognitionEvidence: true);
            }

            return new FeedbackDecision(CatalogRecognitionFeedbackType.Corrected, afterValue, HasRecognitionEvidence: true);
        }

        if (!hasBeforeValue && hasAfterValue)
        {
            return new FeedbackDecision(CatalogRecognitionFeedbackType.AddedManually, afterValue, HasRecognitionEvidence: false);
        }

        return null;
    }

    private static Result<CatalogRecognitionFeedback?, DomainError> CreateFeedback(
        CatalogImportRecognitionFeedbackCollectionRequest request,
        CharacteristicDefinition characteristicDefinition,
        string? beforeValue,
        CatalogImportCharacteristicValueOrigin? beforeOrigin,
        FeedbackDecision decision)
    {
        var productName = decision.HasRecognitionEvidence
            ? request.Before.Name
            : request.After.Name;

        if (string.IsNullOrWhiteSpace(productName))
        {
            return Result.Success<CatalogRecognitionFeedback?, DomainError>(null);
        }

        string? suggestedRawValue = null;
        string? suggestedNormalizedValue = null;
        decimal? suggestedConfidence = null;
        string? suggestedSource = null;
        int? spanStart = null;
        int? spanLength = null;
        Guid? dictionaryTermId = null;
        Guid? recognitionProfileId = null;

        if (decision.HasRecognitionEvidence)
        {
            if (beforeOrigin is null || string.IsNullOrWhiteSpace(beforeValue))
            {
                return Result.Success<CatalogRecognitionFeedback?, DomainError>(null);
            }

            suggestedRawValue = beforeOrigin.RawValue;
            suggestedNormalizedValue = beforeValue;
            suggestedConfidence = beforeOrigin.Confidence;
            suggestedSource = beforeOrigin.RecognitionSource;
            spanStart = beforeOrigin.SpanStart;
            spanLength = beforeOrigin.SpanLength;
            dictionaryTermId = TryExtractGuidAfterToken(beforeOrigin.RecognizerKey, "dictionary");
            recognitionProfileId = TryExtractGuidAfterToken(beforeOrigin.RecognizerKey, "profile");
        }

        var normalizedProductName = CatalogRecognitionTextNormalizer.NormalizeText(productName);

        var createResult = CatalogRecognitionFeedback.Create(
            productName,
            normalizedProductName,
            request.ProductType.Id,
            request.ProductType.Code,
            characteristicDefinition.Id,
            characteristicDefinition.Code,
            suggestedRawValue,
            suggestedNormalizedValue,
            suggestedConfidence,
            suggestedSource,
            spanStart,
            spanLength,
            decision.FeedbackType,
            decision.FinalNormalizedValue,
            dictionaryTermId,
            recognitionProfileId,
            modelVersion: null,
            request.ImportBatchId,
            request.ImportRowId);

        if (createResult.IsFailure)
        {
            return Result.Failure<CatalogRecognitionFeedback?, DomainError>(createResult.Error);
        }

        return Result.Success<CatalogRecognitionFeedback?, DomainError>(createResult.Value);
    }

    private static Dictionary<Guid, string> GetCharacteristicValuesByDefinitionId(IReadOnlyDictionary<string, string> characteristics)
    {
        var values = new Dictionary<Guid, string>();

        foreach (var characteristic in characteristics)
        {
            if (!Guid.TryParse(characteristic.Key, out var characteristicDefinitionId))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(characteristic.Value))
            {
                continue;
            }

            values[characteristicDefinitionId] = characteristic.Value.Trim();
        }

        return values;
    }

    private static Dictionary<Guid, CatalogImportCharacteristicValueOrigin> GetCharacteristicOriginsByDefinitionId(
        IReadOnlyDictionary<string, CatalogImportCharacteristicValueOrigin>? origins)
    {
        var values = new Dictionary<Guid, CatalogImportCharacteristicValueOrigin>();

        if (origins is null)
        {
            return values;
        }

        foreach (var origin in origins)
        {
            if (!Guid.TryParse(origin.Key, out var characteristicDefinitionId))
            {
                continue;
            }

            values[characteristicDefinitionId] = origin.Value;
        }

        return values;
    }

    private static Dictionary<string, CatalogImportCharacteristicValueOrigin> BuildMergedOrigins(
        IReadOnlyDictionary<string, string> afterCharacteristics,
        Dictionary<Guid, string> beforeValues,
        Dictionary<Guid, CatalogImportCharacteristicValueOrigin> beforeOrigins)
    {
        var mergedOrigins = new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.OrdinalIgnoreCase);

        foreach (var characteristic in afterCharacteristics)
        {
            if (!Guid.TryParse(characteristic.Key, out var characteristicDefinitionId))
            {
                mergedOrigins[characteristic.Key] = CatalogImportCharacteristicValueOrigin.FromManual();
                continue;
            }

            var hasBeforeValue = beforeValues.TryGetValue(characteristicDefinitionId, out var beforeValue);
            var valueIsUnchanged = hasBeforeValue && string.Equals(beforeValue, characteristic.Value, StringComparison.Ordinal);

            if (valueIsUnchanged && beforeOrigins.TryGetValue(characteristicDefinitionId, out var previousOrigin))
            {
                mergedOrigins[characteristic.Key] = previousOrigin;
                continue;
            }

            mergedOrigins[characteristic.Key] = CatalogImportCharacteristicValueOrigin.FromManual();
        }

        return mergedOrigins;
    }

    private static bool HasCompleteRecognitionEvidence(CatalogImportCharacteristicValueOrigin origin)
    {
        return !string.IsNullOrWhiteSpace(origin.RawValue) && !string.IsNullOrWhiteSpace(origin.RecognitionSource);
    }

    private static Guid? TryExtractGuidAfterToken(string? recognizerKey, string token)
    {
        if (string.IsNullOrWhiteSpace(recognizerKey))
        {
            return null;
        }

        var parts = recognizerKey.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (!string.Equals(parts[index], token, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Guid.TryParse(parts[index + 1], out var value)
                ? value
                : null;
        }

        return null;
    }

    private sealed record FeedbackDecision(
        CatalogRecognitionFeedbackType FeedbackType,
        string? FinalNormalizedValue,
        bool HasRecognitionEvidence);
}