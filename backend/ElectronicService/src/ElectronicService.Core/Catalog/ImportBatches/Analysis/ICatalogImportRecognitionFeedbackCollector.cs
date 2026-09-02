using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportRecognitionFeedbackCollector
{
    Task<Result<CatalogImportRecognitionFeedbackCollectionResult, DomainError>> CollectAsync(CatalogImportRecognitionFeedbackCollectionRequest request, CancellationToken cancellationToken = default);

    Task<Result<int, DomainError>> RemovePendingForBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default);
}