using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportAnalogyDecisionService
{
    public const string AnalogySuggestedSource = "CatalogAnalogy";
    private readonly PreviewCatalogProductAnalogiesQueryHandler _previewHandler;
    private readonly ICatalogRecognitionFeedbackRepository _feedbackRepository;

    public CatalogImportAnalogyDecisionService(
        PreviewCatalogProductAnalogiesQueryHandler previewHandler,
        ICatalogRecognitionFeedbackRepository feedbackRepository)
    {
        ArgumentNullException.ThrowIfNull(previewHandler);
        ArgumentNullException.ThrowIfNull(feedbackRepository);

        _previewHandler = previewHandler;
        _feedbackRepository = feedbackRepository;
    }

    public async Task<Result<CatalogImportNormalizedRowData, DomainError>> ApplyAsync(
        Guid importBatchId,
        Guid importRowId,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> definitions,
        CatalogImportNormalizedRowData before,
        CatalogImportNormalizedRowData after,
        IReadOnlyCollection<CatalogImportAnalogyDecision> decisions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(decisions);

        if (decisions.Count == 0)
        {
            return after;
        }

        if (importBatchId == Guid.Empty || importRowId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(importRowId));
        }

        if (string.IsNullOrWhiteSpace(after.Name)
            || !after.ManufacturerId.HasValue
            || after.ManufacturerId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired(nameof(after.ManufacturerId));
        }

        var definitionsById = definitions.ToDictionary(definition => definition.Id);
        var decisionDefinitionIds = decisions
            .Select(decision => decision.CharacteristicDefinitionId)
            .ToArray();

        if (decisionDefinitionIds.Any(id => id == Guid.Empty)
            || decisionDefinitionIds.Distinct().Count() != decisionDefinitionIds.Length
            || decisions.Any(decision =>
                decision.Kind == CatalogImportAnalogyDecisionKind.None
                || !Enum.IsDefined(decision.Kind)))
        {
            return GeneralErrors.ValueIsInvalid(nameof(decisions));
        }

        var knownCharacteristics = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (var characteristic in after.Characteristics)
        {
            if (!Guid.TryParse(characteristic.Key, out var definitionId)
                || decisionDefinitionIds.Contains(definitionId)
                || string.IsNullOrWhiteSpace(characteristic.Value)
                || !definitionsById.TryGetValue(definitionId, out var definition))
            {
                continue;
            }

            knownCharacteristics[definition.Code] = characteristic.Value.Trim();
        }

        var preview = await _previewHandler.Handle(
                new CatalogProductAnalogyRequest(
                    after.Name,
                    after.Article,
                    after.ManufacturerId.Value,
                    productType.Id,
                    knownCharacteristics,
                    MaximumResults: 20),
                cancellationToken)
            .ConfigureAwait(false);

        var proposalsByCode = preview.CharacteristicProposals
            .Where(proposal => proposal.IsSuggested)
            .ToDictionary(
                proposal => proposal.CharacteristicCode,
                StringComparer.Ordinal);

        var existingFeedback = await _feedbackRepository
            .GetByImportRowAsync(importRowId, cancellationToken)
            .ConfigureAwait(false);

        var existingByDefinitionId = existingFeedback
            .ToDictionary(feedback => feedback.CharacteristicDefinitionId);

        var origins = after.CharacteristicOrigins is null
            ? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal)
            : after.CharacteristicOrigins.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.Ordinal);

        foreach (var decision in decisions)
        {
            if (!definitionsById.TryGetValue(
                    decision.CharacteristicDefinitionId,
                    out var definition))
            {
                return GeneralErrors.ValueIsInvalid(nameof(decisions));
            }

            if (before.Characteristics.TryGetValue(
                    definition.Id.ToString(),
                    out var previousValue)
                && !string.IsNullOrWhiteSpace(previousValue))
            {
                return GeneralErrors.ValueIsInvalid(nameof(decisions));
            }

            if (!proposalsByCode.TryGetValue(definition.Code, out var proposal))
            {
                return GeneralErrors.ValueIsInvalid(nameof(decisions));
            }

            var finalValue = after.Characteristics.GetValueOrDefault(
                definition.Id.ToString());

            CatalogRecognitionFeedbackType feedbackType;

            if (decision.Kind == CatalogImportAnalogyDecisionKind.Rejected)
            {
                if (string.IsNullOrWhiteSpace(decision.Reason))
                {
                    return GeneralErrors.ValueIsRequired(nameof(decision.Reason));
                }

                feedbackType = CatalogRecognitionFeedbackType.Rejected;
                finalValue = null;
            }
            else if (string.IsNullOrWhiteSpace(finalValue))
            {
                return GeneralErrors.ValueIsRequired(nameof(finalValue));
            }
            else if (string.Equals(
                finalValue.Trim(),
                proposal.ProposedValue,
                StringComparison.OrdinalIgnoreCase))
            {
                feedbackType = CatalogRecognitionFeedbackType.Accepted;
                finalValue = proposal.ProposedValue;
                var supportingProductIds = proposal.SupportingProductIds
                    .ToHashSet();
                var familyAnchor = preview.Matches
                    .Where(match => supportingProductIds.Contains(match.ProductId))
                    .Select(match => match.MatchedFamilyAnchor)
                    .FirstOrDefault(anchor => !string.IsNullOrWhiteSpace(anchor));
                origins[definition.Id.ToString()] =
                    CatalogImportCharacteristicValueOrigin.FromCatalogAnalogy(
                        proposal.ProposedValue,
                        proposal.SupportRatio,
                        preview.ConfigurationId,
                        preview.ConfigurationVersion,
                        proposal.SupportingProductIds,
                        preview.ConfigurationHash,
                        preview.CandidateSnapshotId,
                        preview.CandidateSnapshotCreatedAtUtc,
                        familyAnchor,
                        proposal.SupportingProductCount);
            }
            else
            {
                feedbackType = CatalogRecognitionFeedbackType.Corrected;
                finalValue = finalValue.Trim();
                origins[definition.Id.ToString()] =
                    CatalogImportCharacteristicValueOrigin.FromManual();
            }

            var observation = decision.FeedbackId.HasValue
                ? existingFeedback.SingleOrDefault(item =>
                    item.Id == decision.FeedbackId.Value)
                : null;

            if (decision.FeedbackId.HasValue
                && (observation is null
                    || observation.CharacteristicDefinitionId != definition.Id
                    || observation.ImportBatchId != importBatchId
                    || observation.ImportRowId != importRowId
                    || observation.FeedbackType
                        != CatalogRecognitionFeedbackType.Shown
                    || !string.Equals(
                        observation.SuggestedSource,
                        AnalogySuggestedSource,
                        StringComparison.Ordinal)))
            {
                return GeneralErrors.ValueIsInvalid(nameof(decision.FeedbackId));
            }

            var currentEvidence = CatalogProductAnalogyEvidence.Serialize(
                preview,
                proposal);
            if (observation is not null
                && !observation.MatchesAnalogyObservation(
                    CatalogRecognitionTextNormalizer.NormalizeText(after.Name),
                    proposal.ProposedValue,
                    CatalogProductAnalogyEvidence.AlgorithmVersion,
                    currentEvidence))
            {
                return GeneralErrors.ValueIsInvalid(nameof(decision.FeedbackId));
            }

            var evidenceSnapshotJson = CatalogProductAnalogyEvidence.Serialize(
                preview,
                proposal,
                decision.Reason);

            if (observation is not null)
            {
                var recordDecision = observation.RecordAnalogyDecision(
                    feedbackType,
                    finalValue,
                    evidenceSnapshotJson);
                if (recordDecision.IsFailure)
                {
                    return recordDecision.Error;
                }

                continue;
            }

            if (existingByDefinitionId.TryGetValue(
                    definition.Id,
                    out var storedFeedback))
            {
                if (storedFeedback.IsFinalized)
                {
                    return GeneralErrors.ValueIsInvalid(nameof(decisions));
                }

                _feedbackRepository.Remove(storedFeedback);
            }

            var createResult = CatalogRecognitionFeedback.Create(
                after.Name,
                CatalogRecognitionTextNormalizer.NormalizeText(after.Name),
                after.ManufacturerId.Value,
                productType.Id,
                productType.Code,
                definition.Id,
                definition.Code,
                proposal.ProposedValue,
                proposal.ProposedValue,
                proposal.SupportRatio,
                AnalogySuggestedSource,
                spanStart: null,
                spanLength: null,
                feedbackType,
                finalValue,
                dictionaryTermId: null,
                recognitionProfileId: null,
                CatalogProductAnalogyEvidence.AlgorithmVersion,
                importBatchId,
                importRowId,
                evidenceSnapshotJson);

            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            _feedbackRepository.Add(createResult.Value);
        }

        return after with { CharacteristicOrigins = origins };
    }

}
