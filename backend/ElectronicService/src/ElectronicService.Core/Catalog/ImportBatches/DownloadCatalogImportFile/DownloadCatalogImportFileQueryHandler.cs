using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.DownloadCatalogImportFile;

public sealed class DownloadCatalogImportFileQueryHandler
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public DownloadCatalogImportFileQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<DownloadCatalogImportFileResult, DomainError>> Handle(
        DownloadCatalogImportFileQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        var batch = await _importBatchRepository
            .GetByIdWithFileAsync(query.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var isOwner = batch.CreatedByUserId == currentUser.Id;

        var canDownloadOwnFile =
            isOwner
            && currentUser.CanViewOwnCatalogImports();

        var canDownloadAssignedReviewFile =
            currentUser.CanReviewCatalogImports()
            && batch.ReviewedByUserId == currentUser.Id;

        if (!canDownloadOwnFile && !canDownloadAssignedReviewFile)
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        var content = batch.File.Content;

        if (content.IsEmpty || content.Length != batch.FileSizeBytes)
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.FileIntegrityCheckFailed());
        }

        var calculatedSha256 = Convert.ToHexString(
            SHA256.HashData(content.Span));

        if (!string.Equals(
            calculatedSha256,
            batch.FileSha256,
            StringComparison.Ordinal))
        {
            return Result.Failure<DownloadCatalogImportFileResult, DomainError>(
                CatalogImportErrors.FileIntegrityCheckFailed());
        }

        var result = new DownloadCatalogImportFileResult(
            batch.OriginalFileName,
            ExcelContentType,
            content);

        return Result.Success<DownloadCatalogImportFileResult, DomainError>(
            result);
    }
}