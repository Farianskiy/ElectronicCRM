using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportRecognitionFeedbackFinalizer : ICatalogImportRecognitionFeedbackFinalizer
{
    private readonly ICatalogRecognitionFeedbackRepository _feedbackRepository;

    public CatalogImportRecognitionFeedbackFinalizer(ICatalogRecognitionFeedbackRepository feedbackRepository)
    {
        ArgumentNullException.ThrowIfNull(feedbackRepository);

        _feedbackRepository = feedbackRepository;
    }

    public async Task<Result<int, DomainError>> FinalizeForAppliedBatchAsync(
        Guid importBatchId,
        Guid reviewedByUserId,
        UserType reviewerType,
        CancellationToken cancellationToken = default)
    {
        if (importBatchId == Guid.Empty)
        {
            return Result.Failure<int, DomainError>(GeneralErrors.ValueIsInvalid(nameof(importBatchId)));
        }

        if (reviewedByUserId == Guid.Empty)
        {
            return Result.Failure<int, DomainError>(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (reviewerType != UserType.Technical)
        {
            return Result.Failure<int, DomainError>(GeneralErrors.ValueIsInvalid(nameof(reviewerType)));
        }

        var pendingFeedback = await _feedbackRepository.GetPendingByImportBatchAsync(importBatchId, cancellationToken).ConfigureAwait(false);

        foreach (var feedback in pendingFeedback)
        {
            var finalizationResult = feedback.Finalize(
                CatalogRecognitionLabelQuality.Strong,
                reviewedByUserId,
                reviewerType.ToString(),
                isTrainingEligible: true);

            if (finalizationResult.IsFailure)
            {
                return Result.Failure<int, DomainError>(finalizationResult.Error);
            }
        }

        return pendingFeedback.Count;
    }
}