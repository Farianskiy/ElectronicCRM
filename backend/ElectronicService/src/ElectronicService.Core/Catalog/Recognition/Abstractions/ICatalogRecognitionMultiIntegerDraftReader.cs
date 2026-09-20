using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionMultiIntegerDraftReader
{
    Task<CatalogRecognitionMultiIntegerDraftPage> GetPageAsync(
        Guid manufacturerId,
        Guid productTypeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CatalogRecognitionMultiIntegerDraftEvidencePage?> GetEvidenceAsync(
        Guid draftId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CatalogRecognitionMultiIntegerDraftSnapshot?> GetSnapshotAsync(
        Guid draftId,
        CancellationToken cancellationToken = default);
}