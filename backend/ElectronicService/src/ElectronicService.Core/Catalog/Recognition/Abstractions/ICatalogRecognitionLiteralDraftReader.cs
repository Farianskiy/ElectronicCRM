using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionLiteralDraftReader
{
    Task<CatalogRecognitionLiteralDraftPage> GetPageAsync(CatalogRecognitionTrainingScope scope, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionLiteralDraftDetails?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken = default);
}