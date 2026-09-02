using ElectronicService.Core.Catalog.Recognition.Datasets;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionDatasetReader
{
    IAsyncEnumerable<CatalogRecognitionDatasetRecord> StreamTrainingEligibleAsync(DateTime finalizedUntilUtc, CancellationToken cancellationToken = default);
}