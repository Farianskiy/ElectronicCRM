using ElectronicService.Core.Catalog.Recognition.Datasets;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionDatasetExporter
{
    Task<CatalogRecognitionDatasetExportMetadata> ExportAsync(Stream destination, DateTime finalizedUntilUtc, CancellationToken cancellationToken = default);
}