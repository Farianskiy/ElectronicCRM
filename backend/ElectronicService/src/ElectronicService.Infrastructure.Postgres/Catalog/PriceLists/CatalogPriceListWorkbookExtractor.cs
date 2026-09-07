using System.IO.Compression;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;

public static class CatalogPriceListWorkbookExtractor
{
    private const double MaximumCompressionRatio = 100d;

    public static async Task<
        Result<ReadOnlyMemory<byte>, DomainError>> ExtractAsync(
            string originalFileName,
            ReadOnlyMemory<byte> fileContent,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return GeneralErrors.ValueIsRequired(
                nameof(originalFileName));
        }

        if (fileContent.IsEmpty)
        {
            return CatalogPriceListErrors.FileIsEmpty();
        }

        var safeFileName =
            Path.GetFileName(originalFileName.Trim());

        var extension =
            Path.GetExtension(safeFileName);

        if (string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            if (fileContent.Length
                > CatalogPriceList.MaximumExtractedWorkbookSizeBytes)
            {
                return CatalogPriceListErrors
                    .ExtractedWorkbookIsTooLarge(
                        CatalogPriceList
                            .MaximumExtractedWorkbookSizeBytes);
            }

            return Result.Success<
                ReadOnlyMemory<byte>,
                DomainError>(
                    fileContent);
        }

        if (!string.Equals(
                extension,
                ".zip",
                StringComparison.OrdinalIgnoreCase))
        {
            return CatalogPriceListErrors
                .UnsupportedFileExtension(extension);
        }

        try
        {
            using var archiveStream =
                new MemoryStream(
                    fileContent.ToArray(),
                    writable: false);

            using var archive =
                new ZipArchive(
                    archiveStream,
                    ZipArchiveMode.Read,
                    leaveOpen: false);

            var fileEntries =
                archive.Entries
                    .Where(entry =>
                        !string.IsNullOrEmpty(entry.Name))
                    .ToArray();

            if (fileEntries.Length != 1)
            {
                return CatalogPriceListErrors
                    .ArchiveMustContainSingleWorkbook(
                        fileEntries.Length);
            }

            var workbookEntry =
                fileEntries[0];

            if (HasUnsafePath(workbookEntry.FullName))
            {
                return CatalogPriceListErrors
                    .UnsafeArchiveEntry(
                        workbookEntry.FullName);
            }

            var workbookExtension =
                Path.GetExtension(
                    workbookEntry.Name);

            if (!string.Equals(
                    workbookExtension,
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                return CatalogPriceListErrors
                    .ArchiveMustContainSingleWorkbook(
                        filesCount: 0);
            }

            if (workbookEntry.Length <= 0)
            {
                return CatalogPriceListErrors.FileIsEmpty();
            }

            if (workbookEntry.Length
                > CatalogPriceList
                    .MaximumExtractedWorkbookSizeBytes)
            {
                return CatalogPriceListErrors
                    .ExtractedWorkbookIsTooLarge(
                        CatalogPriceList
                            .MaximumExtractedWorkbookSizeBytes);
            }

            if (HasUnsafeCompressionRatio(
                    workbookEntry))
            {
                return CatalogPriceListErrors
                    .ArchiveCompressionRatioIsTooHigh();
            }

            await using var entryStream =
                await workbookEntry
                    .OpenAsync(cancellationToken)
                    .ConfigureAwait(false);

            using var workbookStream =
                new MemoryStream(
                    capacity: checked(
                        (int)workbookEntry.Length));

            await entryStream
                .CopyToAsync(
                    workbookStream,
                    cancellationToken)
                .ConfigureAwait(false);

            if (workbookStream.Length
                > CatalogPriceList
                    .MaximumExtractedWorkbookSizeBytes)
            {
                return CatalogPriceListErrors
                    .ExtractedWorkbookIsTooLarge(
                        CatalogPriceList
                            .MaximumExtractedWorkbookSizeBytes);
            }

            return Result.Success<
                ReadOnlyMemory<byte>,
                DomainError>(
                    new ReadOnlyMemory<byte>(
                        workbookStream.ToArray()));
        }
        catch (InvalidDataException)
        {
            return CatalogPriceListErrors.InvalidArchive();
        }
        catch (IOException)
        {
            return CatalogPriceListErrors.InvalidArchive();
        }
    }

    private static bool HasUnsafePath(
        string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
        {
            return true;
        }

        var normalizedEntryName =
            entryName.Replace(
                '\\',
                '/');

        if (normalizedEntryName.StartsWith('/'))
        {
            return true;
        }

        if (Path.IsPathRooted(entryName))
        {
            return true;
        }

        var pathSegments =
            normalizedEntryName.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        return pathSegments.Any(segment =>
            string.Equals(
                segment,
                "..",
                StringComparison.Ordinal));
    }

    private static bool HasUnsafeCompressionRatio(
        ZipArchiveEntry entry)
    {
        if (entry.Length == 0)
        {
            return false;
        }

        if (entry.CompressedLength == 0)
        {
            return true;
        }

        var compressionRatio =
            (double)entry.Length
            / entry.CompressedLength;

        return compressionRatio
            > MaximumCompressionRatio;
    }
}