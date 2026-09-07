using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.ApplyCatalogPriceListIssueGroup;

public sealed class ApplyCatalogPriceListIssueGroupCommandHandler
{
    private const int BatchSize = 500;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListRepository
        _priceListRepository;

    public ApplyCatalogPriceListIssueGroupCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceListRepository priceListRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListRepository);

        _userRepository = userRepository;
        _priceListRepository = priceListRepository;
    }

    public async Task<Result<
        ApplyCatalogPriceListIssueGroupResult,
        DomainError>> Handle(
            ApplyCatalogPriceListIssueGroupCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Failure(
                CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!IsValidGroupKey(command.GroupKey))
        {
            return Failure(
                CatalogPriceListErrors.InvalidIssueGroupKey());
        }

        if (command.ProductId.HasValue
            && command.Unit is not null)
        {
            return Failure(
                CatalogPriceListErrors
                    .IssueGroupCorrectionIsAmbiguous());
        }

        if (command.Unit?.Trim().Length
            > CatalogPriceListRow.MaximumUnitLength)
        {
            return Failure(
                CatalogPriceListErrors.IssueGroupUnitTooLong(
                    CatalogPriceListRow.MaximumUnitLength));
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    command.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Failure(
                CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Failure(
                CatalogPriceListErrors
                    .UserCannotEditPriceList());
        }

        var priceList =
            await _priceListRepository
                .GetByIdAsync(
                    command.PriceListId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (priceList is null)
        {
            return Failure(
                CatalogPriceListErrors.PriceListNotFound(
                    command.PriceListId));
        }

        if (priceList.Status
            is not CatalogPriceListStatus.NeedsCorrection
            and not CatalogPriceListStatus.Ready)
        {
            return Failure(
                CatalogPriceListErrors.RowsCannotBeEdited(
                    priceList.Status));
        }

        var isProductCorrection =
            command.ProductId.HasValue;

        if (isProductCorrection)
        {
            var productBelongsToManufacturer =
                await _priceListRepository
                    .ProductBelongsToManufacturerAsync(
                        command.ProductId!.Value,
                        priceList.ManufacturerId,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!productBelongsToManufacturer)
            {
                return Failure(
                    CatalogPriceListErrors
                        .ProductDoesNotBelongToManufacturer(
                            command.ProductId.Value,
                            priceList.ManufacturerId));
            }
        }

        var processedRowsCount = 0;

        while (true)
        {
            var batch =
                await _priceListRepository
                    .GetIssueGroupBatchAsync(
                        priceList.Id,
                        command.GroupKey,
                        BatchSize,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (batch is null)
            {
                if (processedRowsCount == 0)
                {
                    return Failure(
                        CatalogPriceListErrors
                            .IssueGroupNotFound(
                                command.GroupKey));
                }

                break;
            }

            var expectedField =
                isProductCorrection
                    ? nameof(CatalogPriceListRow.ProductId)
                    : nameof(CatalogPriceListRow.Unit);

            if (!string.Equals(
                    batch.Field,
                    expectedField,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    CatalogPriceListErrors
                        .IssueGroupFieldMismatch(
                            batch.Field));
            }

            var rows =
                await _priceListRepository
                    .GetRowsByIdsAsync(
                        priceList.Id,
                        batch.RowIds,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (rows.Count != batch.RowIds.Count)
            {
                var storedRowIds =
                    rows
                        .Select(row => row.Id)
                        .ToHashSet();

                var missingRowId =
                    batch.RowIds.First(
                        rowId =>
                            !storedRowIds.Contains(rowId));

                return Failure(
                    CatalogPriceListErrors
                        .PriceListRowNotFound(
                            missingRowId));
            }

            foreach (var row in rows)
            {
                var correctionResult =
                    row.ApplyCorrection(
                        row.Article,
                        row.Name,
                        row.BasePriceAmount,
                        row.MrcPriceAmount,
                        row.ProductUrl?.OriginalString,
                        isProductCorrection
                            ? row.Unit
                            : command.Unit,
                        isProductCorrection
                            ? command.ProductId
                            : row.ProductId);

                if (correctionResult.IsFailure)
                {
                    return Failure(
                        correctionResult.Error);
                }
            }

            var saveBatchResult =
                await _priceListRepository
                    .SaveCorrectionBatchAsync(
                        rows,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (saveBatchResult.IsFailure)
            {
                return Failure(
                    saveBatchResult.Error);
            }

            processedRowsCount +=
                rows.Count;
        }

        var refreshResult =
            await _priceListRepository
                .SaveCorrectionAndRefreshStatisticsAsync(
                    priceList,
                    cancellationToken)
                .ConfigureAwait(false);

        if (refreshResult.IsFailure)
        {
            return Failure(
                refreshResult.Error);
        }

        return Result.Success<
            ApplyCatalogPriceListIssueGroupResult,
            DomainError>(
                new ApplyCatalogPriceListIssueGroupResult(
                    priceList.Id,
                    priceList.Status,
                    processedRowsCount,
                    priceList.RowsCount,
                    priceList.ValidRowsCount,
                    priceList.ErrorRowsCount));
    }

    private static bool IsValidGroupKey(
        string? groupKey)
    {
        return !string.IsNullOrWhiteSpace(groupKey)
               && groupKey.Trim().Length == 32
               && groupKey
                   .Trim()
                   .All(Uri.IsHexDigit);
    }

    private static Result<
        ApplyCatalogPriceListIssueGroupResult,
        DomainError> Failure(
            DomainError error)
    {
        return Result.Failure<
            ApplyCatalogPriceListIssueGroupResult,
            DomainError>(error);
    }
}