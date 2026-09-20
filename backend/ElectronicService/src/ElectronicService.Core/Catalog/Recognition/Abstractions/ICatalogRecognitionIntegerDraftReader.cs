using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionIntegerDraftReader
{
    Task<CatalogRecognitionIntegerDraftPage> GetPageAsync(CatalogRecognitionTrainingScope scope, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionIntegerDraftEvidencePage?> GetEvidenceAsync(Guid draftId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionIntegerDraftSnapshot?> GetSnapshotAsync(Guid draftId, CancellationToken cancellationToken = default);
}