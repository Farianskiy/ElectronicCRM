using ElectronicService.Core.Catalog.Recognition.Datasets;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionDatasetSplitAssigner
{
    string AlgorithmVersion { get; }

    CatalogRecognitionDatasetSplitAssignment Assign(string normalizedProductName);
}