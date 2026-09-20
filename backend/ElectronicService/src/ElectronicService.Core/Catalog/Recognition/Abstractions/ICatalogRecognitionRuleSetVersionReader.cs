using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionRuleSetVersionReader
{
    Task<CatalogRecognitionRuleSetVersionPage> GetPageAsync(
        Guid manufacturerId,
        Guid productTypeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CatalogRecognitionRuleSetVersionDetails?> GetByIdAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);
}