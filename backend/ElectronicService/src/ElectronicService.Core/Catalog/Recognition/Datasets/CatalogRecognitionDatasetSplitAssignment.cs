namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed record CatalogRecognitionDatasetSplitAssignment(
    CatalogRecognitionDatasetSplit Split,
    string GroupKey,
    string AlgorithmVersion,
    int Bucket);