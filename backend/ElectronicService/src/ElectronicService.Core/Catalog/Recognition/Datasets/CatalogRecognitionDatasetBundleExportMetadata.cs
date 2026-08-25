namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed record CatalogRecognitionDatasetBundleExportMetadata(
    string FileName,
    DateTime ExportedAtUtc,
    CatalogRecognitionDatasetBundleManifest Manifest);