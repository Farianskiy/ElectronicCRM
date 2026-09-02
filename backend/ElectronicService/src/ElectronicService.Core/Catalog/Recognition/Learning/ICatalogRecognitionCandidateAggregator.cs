using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Learning;

public interface ICatalogRecognitionCandidateAggregator
{
    Task<Result<CatalogRecognitionCandidateAggregationResult, DomainError>> AggregateAsync(int batchSize = 500, CancellationToken cancellationToken = default);
}