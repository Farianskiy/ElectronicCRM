using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionFeedbackRepository
{
    Task<CatalogRecognitionFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionFeedback?> GetByImportRowAndCharacteristicAsync(Guid importRowId, Guid characteristicDefinitionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRecognitionFeedback>> GetByImportRowAsync(Guid importRowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRecognitionFeedback>> GetPendingByImportBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForImportRowAndCharacteristicAsync(Guid importRowId, Guid characteristicDefinitionId, CancellationToken cancellationToken = default);

    void Add(CatalogRecognitionFeedback feedback);

    void Remove(CatalogRecognitionFeedback feedback);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}