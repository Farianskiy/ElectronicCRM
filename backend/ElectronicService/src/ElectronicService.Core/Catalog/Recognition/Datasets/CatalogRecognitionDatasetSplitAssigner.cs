using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ElectronicService.Core.Catalog.Recognition.Abstractions;

namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed class CatalogRecognitionDatasetSplitAssigner : ICatalogRecognitionDatasetSplitAssigner
{
    public const string CurrentAlgorithmVersion = "sha256-normalized-product-name-v1";

    private const int TrainBucketUpperBoundary = 80;

    private const int ValidationBucketUpperBoundary = 90;

    public string AlgorithmVersion => CurrentAlgorithmVersion;

    public CatalogRecognitionDatasetSplitAssignment Assign(string normalizedProductName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedProductName);

        var groupKey = normalizedProductName.Trim();
        var hashInput = $"{CurrentAlgorithmVersion}\n{groupKey}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
        var hashPrefix = BinaryPrimitives.ReadUInt32BigEndian(hashBytes);
        var bucket = (int)(hashPrefix % 100u);

        var split = bucket switch
        {
            < TrainBucketUpperBoundary => CatalogRecognitionDatasetSplit.Train,
            < ValidationBucketUpperBoundary => CatalogRecognitionDatasetSplit.Validation,
            _ => CatalogRecognitionDatasetSplit.Test
        };

        return new CatalogRecognitionDatasetSplitAssignment(
            split,
            groupKey,
            CurrentAlgorithmVersion,
            bucket);
    }
}