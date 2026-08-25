using ElectronicService.Core.Catalog.Recognition.Datasets;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionDatasetBundleExporter
{
    Task<CatalogRecognitionDatasetBundleExportMetadata> ExportAsync(Stream destination, DateTime finalizedUntilUtc, CancellationToken cancellationToken = default);
}