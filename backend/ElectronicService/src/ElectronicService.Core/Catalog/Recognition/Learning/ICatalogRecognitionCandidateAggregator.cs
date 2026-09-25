using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Learning;

public interface ICatalogRecognitionCandidateAggregator
{
    Task<Result<CatalogRecognitionCandidateAggregationResult, DomainError>> AggregateAsync(CatalogRecognitionFeedbackType feedbackType, DateTime cutoffUtc, CatalogRecognitionFeedbackCursor? after = null, int batchSize = 500, CancellationToken cancellationToken = default);
}