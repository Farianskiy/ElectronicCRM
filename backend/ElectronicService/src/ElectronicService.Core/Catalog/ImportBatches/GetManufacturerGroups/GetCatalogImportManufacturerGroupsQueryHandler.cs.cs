using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.GetManufacturerGroups;

public sealed class GetCatalogImportManufacturerGroupsQueryHandler
{
    private const int MaximumGroupsCount = 6;

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public GetCatalogImportManufacturerGroupsQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetCatalogImportManufacturerGroupsResult, DomainError>> Handle(
        GetCatalogImportManufacturerGroupsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return CatalogImportErrors.CurrentUserNotFound();
        }

        if (query.BatchId == Guid.Empty)
        {
            return CatalogImportErrors.BatchNotFound(query.BatchId);
        }

        if (!query.ExpectedVersion.HasValue)
        {
            return GeneralErrors.ValueIsRequired(nameof(query.ExpectedVersion));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(
                query.CurrentUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return CatalogImportErrors.CurrentUserNotFound();
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(
                query.BatchId,
                cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return CatalogImportErrors.BatchNotFound(query.BatchId);
        }

        var canRead =
            batch.CreatedByUserId == currentUser.Id
            || currentUser.CanReviewCatalogImports();

        if (!canRead)
        {
            return CatalogImportErrors.UserCannotAccessBatch();
        }

        var version = batch.Version;

        if (version != query.ExpectedVersion.Value)
        {
            return CatalogImportErrors.BatchConcurrencyConflict();
        }

        var groups = await _importBatchRepository
            .GetManufacturerGroupsAsync(
                batch.Id,
                MaximumGroupsCount,
                cancellationToken)
            .ConfigureAwait(false);

        var currentVersion = await _importBatchRepository
            .GetVersionAsync(
                batch.Id,
                cancellationToken)
            .ConfigureAwait(false);

        if (currentVersion != version)
        {
            return CatalogImportErrors.BatchConcurrencyConflict();
        }

        return new GetCatalogImportManufacturerGroupsResult(
            batch.Id,
            version,
            groups
                .Select(group =>
                    new CatalogImportManufacturerGroupResult(
                        group.GroupKey,
                        group.SourceValue,
                        group.ResolutionSource,
                        group.ResolvedManufacturerName,
                        group.ExactNameRowsCount,
                        group.ApprovedAliasRowsCount,
                        group.IgnoredNoiseRowsCount,
                        group.UnresolvedRowsCount,
                        group.ManualRowsCount,
                        group.RowsCount))
                .ToArray());
    }
}