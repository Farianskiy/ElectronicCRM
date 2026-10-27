using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed class CatalogImportAnalogyObservationService
{
    private readonly ICatalogImportBatchRepository _importRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICatalogRecognitionFeedbackRepository _feedbackRepository;

    public CatalogImportAnalogyObservationService(
        ICatalogImportBatchRepository importRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ICatalogRecognitionFeedbackRepository feedbackRepository)
    {
        _importRepository = importRepository;
        _metadataRepository = metadataRepository;
        _feedbackRepository = feedbackRepository;
    }

    public async Task<Result<CatalogProductAnalogyResult, DomainError>> RecordAsync(
        Guid importBatchId,
        Guid importRowId,
        CatalogProductAnalogyRequest request,
        CatalogProductAnalogyResult result,
        CancellationToken cancellationToken = default)
    {
        if (importBatchId == Guid.Empty || importRowId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(importRowId));
        }

        if (!request.ProductTypeId.HasValue
            || request.ProductTypeId.Value == Guid.Empty)
        {
            return result;
        }

        var row = await _importRepository
            .GetRowByIdAsync(importBatchId, importRowId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return GeneralErrors.ValueIsInvalid(nameof(importRowId));
        }

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(
                request.ProductTypeId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (productType is null)
        {
            return GeneralErrors.ValueIsInvalid(nameof(request.ProductTypeId));
        }

        var definitionIds = productType.Characteristics
            .Select(item => item.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var definitions = await _metadataRepository
            .GetCharacteristicDefinitionsByIdsAsync(
                definitionIds,
                cancellationToken)
            .ConfigureAwait(false);
        var definitionsByCode = definitions.ToDictionary(
            item => item.Code,
            StringComparer.Ordinal);
        var existing = await _feedbackRepository
            .GetByImportRowAsync(importRowId, cancellationToken)
            .ConfigureAwait(false);
        var existingByDefinitionId = existing
            .GroupBy(item => item.CharacteristicDefinitionId)
            .ToDictionary(group => group.Key, group => group.First());
        var normalizedName = CatalogRecognitionTextNormalizer.NormalizeText(
            request.ProductName);
        var proposals = new List<CatalogProductAnalogyCharacteristicProposal>();

        foreach (var proposal in result.CharacteristicProposals)
        {
            if (!proposal.IsSuggested
                || !definitionsByCode.TryGetValue(
                    proposal.CharacteristicCode,
                    out var definition))
            {
                proposals.Add(proposal);
                continue;
            }

            var evidence = CatalogProductAnalogyEvidence.Serialize(
                result,
                proposal);
            CatalogRecognitionFeedback feedback;

            if (existingByDefinitionId.TryGetValue(definition.Id, out var stored))
            {
                if (stored.IsFinalized
                    || !string.Equals(
                        stored.SuggestedSource,
                        CatalogImportAnalogyDecisionService.AnalogySuggestedSource,
                        StringComparison.Ordinal)
                    || stored.FeedbackType != CatalogRecognitionFeedbackType.Shown)
                {
                    proposals.Add(proposal);
                    continue;
                }

                if (!stored.MatchesAnalogyObservation(
                        normalizedName,
                        proposal.ProposedValue,
                        CatalogProductAnalogyEvidence.AlgorithmVersion,
                        evidence))
                {
                    var refresh = stored.RefreshAnalogyObservation(
                        request.ProductName,
                        normalizedName,
                        request.ManufacturerId,
                        productType.Id,
                        productType.Code,
                        proposal.ProposedValue,
                        proposal.ProposedValue,
                        proposal.SupportRatio,
                        CatalogImportAnalogyDecisionService.AnalogySuggestedSource,
                        CatalogProductAnalogyEvidence.AlgorithmVersion,
                        evidence,
                        "Изменились входные данные или снимок каталога.");
                    if (refresh.IsFailure)
                    {
                        return refresh.Error;
                    }
                }

                feedback = stored;
            }
            else
            {
                var created = CatalogRecognitionFeedback.Create(
                    request.ProductName,
                    normalizedName,
                    request.ManufacturerId,
                    productType.Id,
                    productType.Code,
                    definition.Id,
                    definition.Code,
                    proposal.ProposedValue,
                    proposal.ProposedValue,
                    proposal.SupportRatio,
                    CatalogImportAnalogyDecisionService.AnalogySuggestedSource,
                    spanStart: null,
                    spanLength: null,
                    CatalogRecognitionFeedbackType.Shown,
                    finalNormalizedValue: null,
                    dictionaryTermId: null,
                    recognitionProfileId: null,
                    CatalogProductAnalogyEvidence.AlgorithmVersion,
                    importBatchId,
                    importRowId,
                    evidence);
                if (created.IsFailure)
                {
                    return created.Error;
                }

                feedback = created.Value;
                _feedbackRepository.Add(feedback);
                existingByDefinitionId[definition.Id] = feedback;
            }

            proposals.Add(proposal with { FeedbackId = feedback.Id });
        }

        await _feedbackRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return result with { CharacteristicProposals = proposals };
    }
}
