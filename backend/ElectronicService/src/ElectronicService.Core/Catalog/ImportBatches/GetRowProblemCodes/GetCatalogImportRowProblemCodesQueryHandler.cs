using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches
    .GetRowProblemCodes;

public sealed class
    GetCatalogImportRowProblemCodesQueryHandler
{
    private const int MaximumSearchLength = 100;

    private readonly ICatalogImportBatchRepository _importBatchRepository;

    private readonly IUserRepository _userRepository;

    public GetCatalogImportRowProblemCodesQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;

        _userRepository = userRepository;
    }

    public async Task<Result<GetCatalogImportRowProblemCodesResult, DomainError>> Handle(
            GetCatalogImportRowProblemCodesQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return CatalogImportErrors
                .CurrentUserNotFound();
        }

        if (query.BatchId == Guid.Empty)
        {
            return CatalogImportErrors
                .BatchNotFound(query.BatchId);
        }

        if (!query.ExpectedVersion.HasValue)
        {
            return GeneralErrors.ValueIsRequired(
                nameof(query.ExpectedVersion));
        }

        var search =
            string.IsNullOrWhiteSpace(query.Search)
                ? null
                : query.Search.Trim();

        if (search?.Length > MaximumSearchLength)
        {
            return CatalogImportErrors
                .RowsSearchIsTooLong(
                    MaximumSearchLength);
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    query.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return CatalogImportErrors
                .CurrentUserNotFound();
        }

        var batch =
            await _importBatchRepository
                .GetByIdAsync(
                    query.BatchId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (batch is null)
        {
            return CatalogImportErrors
                .BatchNotFound(query.BatchId);
        }

        var canRead =
            batch.CreatedByUserId == currentUser.Id
            || currentUser.CanReviewCatalogImports();

        if (!canRead)
        {
            return CatalogImportErrors
                .UserCannotAccessBatch();
        }

        var version = batch.Version;

        if (version != query.ExpectedVersion.Value)
        {
            return CatalogImportErrors
                .BatchConcurrencyConflict();
        }

        var summary =
            await _importBatchRepository
                .GetRowProblemCodesAsync(
                    batch.Id,
                    query.Status,
                    search,
                    cancellationToken)
                .ConfigureAwait(false);

        var currentVersion =
            await _importBatchRepository
                .GetVersionAsync(
                    batch.Id,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentVersion != version)
        {
            return CatalogImportErrors
                .BatchConcurrencyConflict();
        }

        return new GetCatalogImportRowProblemCodesResult(
            batch.Id,
            version,
            summary.TotalRowsCount,
            summary.ErrorRowsCount,
            summary.WarningRowsCount,
            summary.Items
                .Select(item =>
                    new CatalogImportRowProblemCodeResult(
                        item.Code,
                        item.RowsCount,
                        item.ErrorRowsCount,
                        item.WarningRowsCount))
                .ToArray());
    }
}