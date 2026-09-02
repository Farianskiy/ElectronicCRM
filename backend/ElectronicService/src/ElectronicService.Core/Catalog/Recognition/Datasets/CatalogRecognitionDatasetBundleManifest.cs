namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed record CatalogRecognitionDatasetBundleManifest(
    string BundleFormatVersion,
    string DatasetFormatVersion,
    DateTime FinalizedUntilUtc,
    string SplitAlgorithmVersion,
    int ProductGroupCount,
    long ExampleCount,
    IReadOnlyCollection<CatalogRecognitionDatasetSplitManifest> Splits,
    string DatasetSha256,
    IReadOnlyCollection<CatalogRecognitionDatasetBundleWarning> Warnings);

public sealed record CatalogRecognitionDatasetSplitManifest(
    string Split,
    string FileName,
    long ExampleCount,
    int ProductGroupCount,
    string Sha256);

public sealed record CatalogRecognitionDatasetBundleWarning(
    string Code,
    string Message);