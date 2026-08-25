using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Learning;

public sealed class CatalogRecognitionCandidateSuggestionPromoter : ICatalogRecognitionCandidateSuggestionPromoter
{
    public const int MinimumBatchSize = 1;

    public const int MaximumBatchSize = 5000;

    private readonly ICatalogRecognitionCandidateRepository _candidateRepository;
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CatalogRecognitionCandidateSuggestionPromoter(
        ICatalogRecognitionCandidateRepository candidateRepository,
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(candidateRepository);
        ArgumentNullException.ThrowIfNull(suggestionRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _candidateRepository = candidateRepository;
        _suggestionRepository = suggestionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>> PromoteEligibleAsync(Guid createdByUserId, int batchSize = 500, CancellationToken cancellationToken = default)
    {
        if (createdByUserId == Guid.Empty)
        {
            return Result.Failure<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>(GeneralErrors.ValueIsInvalid(nameof(createdByUserId)));
        }

        if (batchSize is < MinimumBatchSize or > MaximumBatchSize)
        {
            return Result.Failure<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>(GeneralErrors.ValueIsInvalid(nameof(batchSize)));
        }

        var candidates = await _candidateRepository.GetAccumulatingCandidatesAsync(batchSize, cancellationToken).ConfigureAwait(false);

        var eligibleCandidateCount = 0;
        var createdSuggestionCount = 0;
        var attachedExistingSuggestionCount = 0;
        var deferredCandidateCount = 0;

        foreach (var candidate in candidates)
        {
            if (!CatalogRecognitionCandidateSuggestionPolicy.IsEligible(candidate))
            {
                deferredCandidateCount++;
                continue;
            }

            eligibleCandidateCount++;

            var confidence = CatalogRecognitionCandidateSuggestionPolicy.CalculateConfidence(candidate);

            var suggestionResult = CatalogAssistantDictionarySuggestion.CreateFromRecognitionCandidate(
                candidate,
                confidence,
                createdByUserId);

            if (suggestionResult.IsFailure)
            {
                return Result.Failure<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>(suggestionResult.Error);
            }

            var generatedSuggestion = suggestionResult.Value;

            var equivalentPendingSuggestion = await _suggestionRepository.GetEquivalentPendingAsync(
                generatedSuggestion,
                cancellationToken).ConfigureAwait(false);

            Guid suggestionId;

            if (equivalentPendingSuggestion is null)
            {
                _suggestionRepository.Add(generatedSuggestion);
                suggestionId = generatedSuggestion.Id;
                createdSuggestionCount++;
            }
            else
            {
                var alreadyLinkedCandidate = await _candidateRepository.GetBySuggestionIdAsync(
                    equivalentPendingSuggestion.Id,
                    cancellationToken).ConfigureAwait(false);

                if (alreadyLinkedCandidate is not null)
                {
                    deferredCandidateCount++;
                    continue;
                }

                suggestionId = equivalentPendingSuggestion.Id;
                attachedExistingSuggestionCount++;
            }

            var attachResult = candidate.AttachSuggestion(suggestionId);

            if (attachResult.IsFailure)
            {
                return Result.Failure<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>(attachResult.Error);
            }
        }

        if (createdSuggestionCount > 0 || attachedExistingSuggestionCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>(
            new CatalogRecognitionCandidateSuggestionPromotionResult(
                candidates.Count,
                eligibleCandidateCount,
                createdSuggestionCount,
                attachedExistingSuggestionCount,
                deferredCandidateCount));
    }
}