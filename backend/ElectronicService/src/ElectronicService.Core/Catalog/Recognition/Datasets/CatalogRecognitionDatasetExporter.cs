using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using ElectronicService.Core.Catalog.Recognition.Abstractions;

namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed class CatalogRecognitionDatasetExporter : ICatalogRecognitionDatasetExporter
{
    public const string CurrentFormatVersion = CatalogRecognitionDatasetJsonLineSerializer.CurrentFormatVersion;

    private readonly ICatalogRecognitionDatasetReader _datasetReader;

    public CatalogRecognitionDatasetExporter(ICatalogRecognitionDatasetReader datasetReader)
    {
        ArgumentNullException.ThrowIfNull(datasetReader);

        _datasetReader = datasetReader;
    }

    public async Task<CatalogRecognitionDatasetExportMetadata> ExportAsync(Stream destination, DateTime finalizedUntilUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException("Dataset destination stream must be writable.", nameof(destination));
        }

        if (destination.CanSeek && (destination.Position != 0 || destination.Length != 0))
        {
            throw new ArgumentException("Dataset destination stream must be empty and positioned at the beginning.", nameof(destination));
        }

        if (finalizedUntilUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Dataset cutoff must use UTC.", nameof(finalizedUntilUtc));
        }

        var exampleCounts = CreateEmptyExampleCounts();
        long exampleCount = 0;

        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        await foreach (var record in _datasetReader.StreamTrainingEligibleAsync(finalizedUntilUtc, cancellationToken).ConfigureAwait(false))
        {
            var lineBytes = CatalogRecognitionDatasetJsonLineSerializer.Serialize(record);

            await destination.WriteAsync(lineBytes, cancellationToken).ConfigureAwait(false);

            sha256.AppendData(lineBytes);
            exampleCount++;
            exampleCounts[record.ExampleKind.ToString()]++;
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);

        var hash = Convert.ToHexString(sha256.GetHashAndReset());
        var fileName = BuildFileName(finalizedUntilUtc);

        return new CatalogRecognitionDatasetExportMetadata(
            CurrentFormatVersion,
            fileName,
            finalizedUntilUtc,
            DateTime.UtcNow,
            exampleCount,
            new ReadOnlyDictionary<string, long>(exampleCounts),
            hash);
    }

    private static Dictionary<string, long> CreateEmptyExampleCounts()
    {
        return Enum.GetValues<CatalogRecognitionDatasetExampleKind>()
            .Where(exampleKind => exampleKind != CatalogRecognitionDatasetExampleKind.None)
            .ToDictionary(exampleKind => exampleKind.ToString(), _ => 0L, StringComparer.Ordinal);
    }

    private static string BuildFileName(DateTime finalizedUntilUtc)
    {
        var cutoff = finalizedUntilUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        return $"catalog-recognition-dataset-{cutoff}.jsonl";
    }
}