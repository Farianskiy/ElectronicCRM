using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ElectronicService.Core.Catalog.Recognition.Abstractions;

namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed class CatalogRecognitionDatasetBundleExporter : ICatalogRecognitionDatasetBundleExporter
{
    public const string CurrentBundleFormatVersion = "1.0";

    public const int MinimumRecommendedProductGroupCount = 30;

    private const int CopyBufferSize = 65536;

    private static readonly JsonSerializerOptions ManifestSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ICatalogRecognitionDatasetReader _datasetReader;

    private readonly ICatalogRecognitionDatasetSplitAssigner _splitAssigner;

    public CatalogRecognitionDatasetBundleExporter(
        ICatalogRecognitionDatasetReader datasetReader,
        ICatalogRecognitionDatasetSplitAssigner splitAssigner)
    {
        ArgumentNullException.ThrowIfNull(datasetReader);
        ArgumentNullException.ThrowIfNull(splitAssigner);

        _datasetReader = datasetReader;
        _splitAssigner = splitAssigner;
    }

    public async Task<CatalogRecognitionDatasetBundleExportMetadata> ExportAsync(Stream destination, DateTime finalizedUntilUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.CanWrite)
        {
            throw new ArgumentException("Dataset bundle destination stream must be writable.", nameof(destination));
        }

        if (destination.CanSeek && (destination.Position != 0 || destination.Length != 0))
        {
            throw new ArgumentException("Dataset bundle destination stream must be empty and positioned at the beginning.", nameof(destination));
        }

        if (finalizedUntilUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Dataset cutoff must use UTC.", nameof(finalizedUntilUtc));
        }

        await using var trainStream = CreateTemporaryStream("train");
        await using var validationStream = CreateTemporaryStream("validation");
        await using var testStream = CreateTemporaryStream("test");

        using var trainHashBuilder = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var validationHashBuilder = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var testHashBuilder = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var allProductGroups = new HashSet<string>(StringComparer.Ordinal);
        var trainProductGroups = new HashSet<string>(StringComparer.Ordinal);
        var validationProductGroups = new HashSet<string>(StringComparer.Ordinal);
        var testProductGroups = new HashSet<string>(StringComparer.Ordinal);

        long trainExampleCount = 0;
        long validationExampleCount = 0;
        long testExampleCount = 0;

        await foreach (var record in _datasetReader.StreamTrainingEligibleAsync(finalizedUntilUtc, cancellationToken).ConfigureAwait(false))
        {
            var assignment = _splitAssigner.Assign(record.NormalizedProductName);
            var lineBytes = CatalogRecognitionDatasetJsonLineSerializer.Serialize(record);

            allProductGroups.Add(assignment.GroupKey);

            switch (assignment.Split)
            {
                case CatalogRecognitionDatasetSplit.Train:
                    await WriteLineAsync(trainStream, trainHashBuilder, lineBytes, cancellationToken).ConfigureAwait(false);
                    trainProductGroups.Add(assignment.GroupKey);
                    trainExampleCount++;
                    break;

                case CatalogRecognitionDatasetSplit.Validation:
                    await WriteLineAsync(validationStream, validationHashBuilder, lineBytes, cancellationToken).ConfigureAwait(false);
                    validationProductGroups.Add(assignment.GroupKey);
                    validationExampleCount++;
                    break;

                case CatalogRecognitionDatasetSplit.Test:
                    await WriteLineAsync(testStream, testHashBuilder, lineBytes, cancellationToken).ConfigureAwait(false);
                    testProductGroups.Add(assignment.GroupKey);
                    testExampleCount++;
                    break;

                default:
                    throw new InvalidOperationException($"Dataset split assigner returned unsupported split '{assignment.Split}'.");
            }
        }

        await trainStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        await validationStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        await testStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        var trainSha256 = Convert.ToHexString(trainHashBuilder.GetHashAndReset());
        var validationSha256 = Convert.ToHexString(validationHashBuilder.GetHashAndReset());
        var testSha256 = Convert.ToHexString(testHashBuilder.GetHashAndReset());

        var splitManifests = new[]
        {
            new CatalogRecognitionDatasetSplitManifest(
                CatalogRecognitionDatasetSplit.Train.ToString(),
                "train.jsonl",
                trainExampleCount,
                trainProductGroups.Count,
                trainSha256),
            new CatalogRecognitionDatasetSplitManifest(
                CatalogRecognitionDatasetSplit.Validation.ToString(),
                "validation.jsonl",
                validationExampleCount,
                validationProductGroups.Count,
                validationSha256),
            new CatalogRecognitionDatasetSplitManifest(
                CatalogRecognitionDatasetSplit.Test.ToString(),
                "test.jsonl",
                testExampleCount,
                testProductGroups.Count,
                testSha256)
        };

        var warnings = CreateWarnings(
            allProductGroups.Count,
            trainExampleCount,
            validationExampleCount,
            testExampleCount);

        var datasetSha256 = CalculateDatasetSha256(splitManifests);
        var totalExampleCount = trainExampleCount + validationExampleCount + testExampleCount;

        var manifest = new CatalogRecognitionDatasetBundleManifest(
            CurrentBundleFormatVersion,
            CatalogRecognitionDatasetJsonLineSerializer.CurrentFormatVersion,
            finalizedUntilUtc,
            _splitAssigner.AlgorithmVersion,
            allProductGroups.Count,
            totalExampleCount,
            splitManifests,
            datasetSha256,
            warnings);

        trainStream.Position = 0;
        validationStream.Position = 0;
        testStream.Position = 0;

        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            await AddStreamEntryAsync(archive, "train.jsonl", trainStream, finalizedUntilUtc, cancellationToken).ConfigureAwait(false);
            await AddStreamEntryAsync(archive, "validation.jsonl", validationStream, finalizedUntilUtc, cancellationToken).ConfigureAwait(false);
            await AddStreamEntryAsync(archive, "test.jsonl", testStream, finalizedUntilUtc, cancellationToken).ConfigureAwait(false);
            await AddManifestEntryAsync(archive, manifest, finalizedUntilUtc, cancellationToken).ConfigureAwait(false);
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);

        return new CatalogRecognitionDatasetBundleExportMetadata(
            BuildBundleFileName(finalizedUntilUtc),
            DateTime.UtcNow,
            manifest);
    }

    private static async Task WriteLineAsync(Stream destination, IncrementalHash hashBuilder, byte[] lineBytes, CancellationToken cancellationToken)
    {
        await destination.WriteAsync(lineBytes, cancellationToken).ConfigureAwait(false);
        hashBuilder.AppendData(lineBytes);
    }

    private static async Task AddStreamEntryAsync(
        ZipArchive archive,
        string entryName,
        Stream source,
        DateTime finalizedUntilUtc,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        entry.LastWriteTime = GetZipEntryTimestamp(finalizedUntilUtc);

        await using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);

        await source.CopyToAsync(entryStream, CopyBufferSize, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AddManifestEntryAsync(
        ZipArchive archive,
        CatalogRecognitionDatasetBundleManifest manifest,
        DateTime finalizedUntilUtc,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
        entry.LastWriteTime = GetZipEntryTimestamp(finalizedUntilUtc);

        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestSerializerOptions);
        var manifestBytesWithNewLine = new byte[manifestBytes.Length + 1];

        manifestBytes.CopyTo(manifestBytesWithNewLine, 0);
        manifestBytesWithNewLine[^1] = (byte)'\n';

        await using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);

        await entryStream.WriteAsync(manifestBytesWithNewLine, cancellationToken).ConfigureAwait(false);
    }

    private static FileStream CreateTemporaryStream(string splitName)
    {
        var temporaryFilePath = Path.Combine(
            Path.GetTempPath(),
            $"electroniccrm-catalog-recognition-{splitName}-{Path.GetRandomFileName()}");

        return new FileStream(
            temporaryFilePath,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.ReadWrite,
                Share = FileShare.Read | FileShare.Delete,
                BufferSize = CopyBufferSize,
                Options = FileOptions.Asynchronous |
                    FileOptions.SequentialScan |
                    FileOptions.DeleteOnClose
            });
    }

    private static List<CatalogRecognitionDatasetBundleWarning> CreateWarnings(
        int productGroupCount,
        long trainExampleCount,
        long validationExampleCount,
        long testExampleCount)
    {
        var warnings = new List<CatalogRecognitionDatasetBundleWarning>();

        if (productGroupCount < MinimumRecommendedProductGroupCount)
        {
            warnings.Add(new CatalogRecognitionDatasetBundleWarning(
                "dataset.too_few_product_groups",
                $"Датасет содержит только {productGroupCount} независимых групп товаров. Рекомендуется накопить не менее {MinimumRecommendedProductGroupCount}."));
        }

        if (trainExampleCount == 0)
        {
            warnings.Add(new CatalogRecognitionDatasetBundleWarning(
                "dataset.empty_train_split",
                "Train-набор пуст. Обучение модели невозможно."));
        }

        if (validationExampleCount == 0)
        {
            warnings.Add(new CatalogRecognitionDatasetBundleWarning(
                "dataset.empty_validation_split",
                "Validation-набор пуст. Нельзя безопасно выбирать параметры и версию модели."));
        }

        if (testExampleCount == 0)
        {
            warnings.Add(new CatalogRecognitionDatasetBundleWarning(
                "dataset.empty_test_split",
                "Test-набор пуст. Нельзя получить независимую итоговую оценку модели."));
        }

        return warnings;
    }

    private static string CalculateDatasetSha256(IReadOnlyCollection<CatalogRecognitionDatasetSplitManifest> splitManifests)
    {
        var canonicalDescription = string.Join(
            "\n",
            splitManifests.Select(split => $"{split.FileName}:{split.Sha256}"));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalDescription)));
    }

    private static DateTimeOffset GetZipEntryTimestamp(DateTime finalizedUntilUtc)
    {
        var minimumZipTimestampUtc = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var effectiveTimestampUtc = finalizedUntilUtc < minimumZipTimestampUtc ? minimumZipTimestampUtc : finalizedUntilUtc;

        return new DateTimeOffset(effectiveTimestampUtc);
    }

    private static string BuildBundleFileName(DateTime finalizedUntilUtc)
    {
        var cutoff = finalizedUntilUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        return $"catalog-recognition-dataset-{cutoff}.zip";
    }
}