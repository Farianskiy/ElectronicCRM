using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionTrainingExampleRepository
{
    Task<CatalogRecognitionTrainingExample> AddOrGetActiveAsync(CatalogRecognitionTrainingExample example, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionTrainingExample?> GetByIdAsync(Guid exampleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRecognitionTrainingExample>> GetActiveByFeedbackIdsAsync(IReadOnlyCollection<Guid> feedbackIds, CancellationToken cancellationToken = default);
}
