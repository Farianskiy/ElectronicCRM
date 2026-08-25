namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed record CatalogRecognitionDatasetExportMetadata(
    string FormatVersion,
    string FileName,
    DateTime FinalizedUntilUtc,
    DateTime ExportedAtUtc,
    long ExampleCount,
    IReadOnlyDictionary<string, long> ExampleCounts,
    string Sha256);