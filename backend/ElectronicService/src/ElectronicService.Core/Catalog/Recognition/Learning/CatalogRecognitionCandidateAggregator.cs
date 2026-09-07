using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Learning;

public sealed class CatalogRecognitionCandidateAggregator : ICatalogRecognitionCandidateAggregator
{
    public const int MinimumBatchSize = 1;

    public const int MaximumBatchSize = 5000;

    private readonly ICatalogRecognitionCandidateRepository _candidateRepository;

    public CatalogRecognitionCandidateAggregator(ICatalogRecognitionCandidateRepository candidateRepository)
    {
        ArgumentNullException.ThrowIfNull(candidateRepository);

        _candidateRepository = candidateRepository;
    }

    public async Task<Result<CatalogRecognitionCandidateAggregationResult, DomainError>> AggregateAsync(int batchSize = 500, CancellationToken cancellationToken = default)
    {
        if (batchSize is < MinimumBatchSize or > MaximumBatchSize)
        {
            return Result.Failure<CatalogRecognitionCandidateAggregationResult, DomainError>(GeneralErrors.ValueIsInvalid(nameof(batchSize)));
        }

        var correctedFeedback = await _candidateRepository.GetUnprocessedFinalizedFeedbackAsync(
            CatalogRecognitionFeedbackType.Corrected,
            batchSize,
            cancellationToken).ConfigureAwait(false);

        var acceptedFeedback = await _candidateRepository.GetUnprocessedFinalizedFeedbackAsync(
            CatalogRecognitionFeedbackType.Accepted,
            batchSize,
            cancellationToken).ConfigureAwait(false);

        var rejectedFeedback = await _candidateRepository.GetUnprocessedFinalizedFeedbackAsync(
            CatalogRecognitionFeedbackType.Rejected,
            batchSize,
            cancellationToken).ConfigureAwait(false);

        var feedbackEntries = new List<CatalogRecognitionFeedback>(
            correctedFeedback.Count + acceptedFeedback.Count + rejectedFeedback.Count);

        feedbackEntries.AddRange(correctedFeedback);
        feedbackEntries.AddRange(acceptedFeedback);
        feedbackEntries.AddRange(rejectedFeedback);

        var createdCandidateCount = 0;
        var addedEvidenceCount = 0;
        var alreadyProcessedFeedbackCount = 0;
        var deferredFeedbackCount = 0;

        var updatedCandidateIds = new HashSet<Guid>();
        var productsObservedDuringCurrentRun = new HashSet<CandidateProductKey>();

        foreach (var feedback in feedbackEntries)
        {
            var identity = BuildCandidateIdentity(feedback);

            if (identity is null)
            {
                deferredFeedbackCount++;
                continue;
            }

            var evidenceAlreadyExists = await _candidateRepository.HasEvidenceForFeedbackAsync(feedback.Id, cancellationToken).ConfigureAwait(false);

            if (evidenceAlreadyExists)
            {
                alreadyProcessedFeedbackCount++;
                continue;
            }

            var candidate = await _candidateRepository.GetByCandidateKeyAsync(identity.CandidateKey, cancellationToken).ConfigureAwait(false);

            if (candidate is null)
            {
                if (feedback.FeedbackType != CatalogRecognitionFeedbackType.Corrected)
                {
                    deferredFeedbackCount++;
                    continue;
                }

                var createCandidateResult = CatalogRecognitionCandidate.Create(
                    identity.Phrase,
                    identity.NormalizedPhrase,
                    feedback.ManufacturerId!.Value,
                    feedback.ProductTypeId,
                    feedback.ProductTypeCodeSnapshot,
                    feedback.CharacteristicDefinitionId,
                    feedback.CharacteristicCodeSnapshot,
                    identity.ProposedValue,
                    feedback.FeedbackType,
                    identity.SeenAtUtc);

                if (createCandidateResult.IsFailure)
                {
                    return Result.Failure<CatalogRecognitionCandidateAggregationResult, DomainError>(createCandidateResult.Error);
                }

                candidate = createCandidateResult.Value;

                var createEvidenceResult = CatalogRecognitionCandidateEvidence.Create(candidate.Id, feedback.Id);

                if (createEvidenceResult.IsFailure)
                {
                    return Result.Failure<CatalogRecognitionCandidateAggregationResult, DomainError>(createEvidenceResult.Error);
                }

                _candidateRepository.AddCandidate(candidate);
                _candidateRepository.AddEvidence(createEvidenceResult.Value);

                productsObservedDuringCurrentRun.Add(new CandidateProductKey(candidate.Id, feedback.NormalizedProductName));

                createdCandidateCount++;
                addedEvidenceCount++;

                continue;
            }

            if (!CandidateMatchesIdentity(candidate, feedback, identity))
            {
                return Result.Failure<CatalogRecognitionCandidateAggregationResult, DomainError>(
                    new DomainError(
                        "catalog.recognition_candidate_key_collision",
                        $"Обнаружено несовпадение данных для CandidateKey '{identity.CandidateKey}'."));
            }

            var candidateProductKey = new CandidateProductKey(candidate.Id, feedback.NormalizedProductName);
            var productWasAlreadyObservedDuringCurrentRun = productsObservedDuringCurrentRun.Contains(candidateProductKey);

            var productWasAlreadyObservedInDatabase = false;

            if (!productWasAlreadyObservedDuringCurrentRun)
            {
                productWasAlreadyObservedInDatabase = await _candidateRepository.HasEvidenceForNormalizedProductNameAsync(
                    candidate.Id,
                    feedback.NormalizedProductName,
                    cancellationToken).ConfigureAwait(false);
            }

            var isDistinctProduct = !productWasAlreadyObservedDuringCurrentRun && !productWasAlreadyObservedInDatabase;

            var addEvidenceResult = candidate.AddEvidence(feedback.FeedbackType, identity.SeenAtUtc, isDistinctProduct);

            if (addEvidenceResult.IsFailure)
            {
                return Result.Failure<CatalogRecognitionCandidateAggregationResult, DomainError>(addEvidenceResult.Error);
            }

            var createAdditionalEvidenceResult = CatalogRecognitionCandidateEvidence.Create(candidate.Id, feedback.Id);

            if (createAdditionalEvidenceResult.IsFailure)
            {
                return Result.Failure<CatalogRecognitionCandidateAggregationResult, DomainError>(createAdditionalEvidenceResult.Error);
            }

            _candidateRepository.AddEvidence(createAdditionalEvidenceResult.Value);

            productsObservedDuringCurrentRun.Add(candidateProductKey);
            updatedCandidateIds.Add(candidate.Id);
            addedEvidenceCount++;
        }

        if (addedEvidenceCount > 0)
        {
            await _candidateRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new CatalogRecognitionCandidateAggregationResult(
            feedbackEntries.Count,
            createdCandidateCount,
            updatedCandidateIds.Count,
            addedEvidenceCount,
            alreadyProcessedFeedbackCount,
            deferredFeedbackCount);
    }

    private static CandidateIdentity? BuildCandidateIdentity(CatalogRecognitionFeedback feedback)
    {
        if (!feedback.IsFinalized || !feedback.IsTrainingEligible || feedback.FinalizedAtUtc is null)
        {
            return null;
        }

        if (!feedback.ManufacturerId.HasValue || feedback.ManufacturerId.Value == Guid.Empty)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(feedback.SuggestedRawValue))
        {
            return null;
        }

        var proposedValue = feedback.FeedbackType switch
        {
            CatalogRecognitionFeedbackType.Accepted => feedback.FinalNormalizedValue,
            CatalogRecognitionFeedbackType.Corrected => feedback.FinalNormalizedValue,
            CatalogRecognitionFeedbackType.Rejected => feedback.SuggestedNormalizedValue,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(proposedValue))
        {
            return null;
        }

        var phrase = feedback.SuggestedRawValue.Trim();
        var normalizedPhrase = CatalogRecognitionTextNormalizer.NormalizeText(phrase);
        var trimmedProposedValue = proposedValue.Trim();

        var candidateKey = CatalogRecognitionCandidate.BuildCandidateKey(
            normalizedPhrase,
            feedback.ManufacturerId.Value,
            feedback.ProductTypeId,
            feedback.CharacteristicDefinitionId,
            trimmedProposedValue);

        return new CandidateIdentity(
            phrase,
            normalizedPhrase,
            trimmedProposedValue,
            candidateKey,
            feedback.FinalizedAtUtc.Value);
    }

    private static bool CandidateMatchesIdentity(CatalogRecognitionCandidate candidate, CatalogRecognitionFeedback feedback, CandidateIdentity identity)
    {
        return candidate.ManufacturerId == feedback.ManufacturerId
            && candidate.ProductTypeId == feedback.ProductTypeId
            && candidate.CharacteristicDefinitionId == feedback.CharacteristicDefinitionId
            && string.Equals(candidate.NormalizedPhrase, identity.NormalizedPhrase, StringComparison.Ordinal)
            && string.Equals(candidate.ProposedValue, identity.ProposedValue, StringComparison.Ordinal);
    }

    private sealed record CandidateIdentity(
        string Phrase,
        string NormalizedPhrase,
        string ProposedValue,
        string CandidateKey,
        DateTime SeenAtUtc);

    private readonly record struct CandidateProductKey(Guid CandidateId, string NormalizedProductName);
}