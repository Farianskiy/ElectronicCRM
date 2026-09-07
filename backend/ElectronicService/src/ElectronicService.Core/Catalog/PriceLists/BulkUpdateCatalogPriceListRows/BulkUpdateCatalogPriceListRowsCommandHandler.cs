using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.BulkUpdateCatalogPriceListRows;

public sealed class BulkUpdateCatalogPriceListRowsCommandHandler
{
    public const int MaximumRowsCount = 200;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListRepository
        _priceListRepository;

    public BulkUpdateCatalogPriceListRowsCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceListRepository priceListRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListRepository);

        _userRepository = userRepository;
        _priceListRepository = priceListRepository;
    }

    public async Task<Result<
        BulkUpdateCatalogPriceListRowsResult,
        DomainError>> Handle(
            BulkUpdateCatalogPriceListRowsCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Rows);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (command.Rows.Count == 0)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.BulkRowsRequired());
        }

        if (command.Rows.Count > MaximumRowsCount)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors
                        .BulkRowsLimitExceeded(
                            MaximumRowsCount));
        }

        var duplicateRowId =
            command.Rows
                .GroupBy(row =>
                    row.RowId)
                .Where(group =>
                    group.Count() > 1)
                .Select(group =>
                    group.Key)
                .FirstOrDefault();

        if (duplicateRowId != Guid.Empty)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.DuplicateBulkRow(
                        duplicateRowId));
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    command.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
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
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        command.PriceListId));
        }

        if (priceList.Status
            is not CatalogPriceListStatus.NeedsCorrection
            and not CatalogPriceListStatus.Ready)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.RowsCannotBeEdited(
                        priceList.Status));
        }

        var rowIds =
            command.Rows
                .Select(row =>
                    row.RowId)
                .ToArray();

        var storedRows =
            await _priceListRepository
                .GetRowsByIdsAsync(
                    priceList.Id,
                    rowIds,
                    cancellationToken)
                .ConfigureAwait(false);

        if (storedRows.Count != rowIds.Length)
        {
            var storedRowIds =
                storedRows
                    .Select(row =>
                        row.Id)
                    .ToHashSet();

            var missingRowId =
                rowIds.First(
                    rowId =>
                        !storedRowIds.Contains(rowId));

            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListRowNotFound(
                        missingRowId));
        }

        var requestedProductIds =
            command.Rows
                .Where(row =>
                    row.ProductId.HasValue)
                .Select(row =>
                    row.ProductId!.Value)
                .Distinct()
                .ToArray();

        if (requestedProductIds.Length > 0)
        {
            var existingProductIds =
                await _priceListRepository
                    .GetProductIdsByManufacturerAsync(
                        priceList.ManufacturerId,
                        requestedProductIds,
                        cancellationToken)
                    .ConfigureAwait(false);

            var existingProductIdSet =
                existingProductIds.ToHashSet();

            var invalidProductId =
                requestedProductIds
                    .FirstOrDefault(
                        productId =>
                            !existingProductIdSet.Contains(
                                productId));

            if (invalidProductId != Guid.Empty)
            {
                return Result.Failure<
                    BulkUpdateCatalogPriceListRowsResult,
                    DomainError>(
                        CatalogPriceListErrors
                            .ProductDoesNotBelongToManufacturer(
                                invalidProductId,
                                priceList.ManufacturerId));
            }
        }

        var storedRowsById =
            storedRows.ToDictionary(
                row =>
                    row.Id);

        foreach (var requestedRow in command.Rows)
        {
            var storedRow =
                storedRowsById[requestedRow.RowId];

            var correctionResult =
                storedRow.ApplyCorrection(
                    requestedRow.Article,
                    requestedRow.Name,
                    requestedRow.BasePriceAmount,
                    requestedRow.MrcPriceAmount,
                    requestedRow.ProductUrl?.OriginalString,
                    requestedRow.Unit,
                    requestedRow.ProductId);

            if (correctionResult.IsFailure)
            {
                return Result.Failure<
                    BulkUpdateCatalogPriceListRowsResult,
                    DomainError>(
                        correctionResult.Error);
            }
        }

        var saveResult =
            await _priceListRepository
                .SaveCorrectionAndRefreshStatisticsAsync(
                    priceList,
                    cancellationToken)
                .ConfigureAwait(false);

        if (saveResult.IsFailure)
        {
            return Result.Failure<
                BulkUpdateCatalogPriceListRowsResult,
                DomainError>(
                    saveResult.Error);
        }

        var resultRows =
            storedRows
                .Select(
                    row =>
                        new CatalogPriceListRowDetails(
                            row.Id,
                            row.RowNumber,
                            row.Article,
                            row.Name,
                            row.BasePriceAmount,
                            row.MrcPriceAmount,
                            row.ProductUrl,
                            row.Unit,
                            row.ProductId,
                            row.Status,
                            row.MatchStatus,
                            row.MatchConfidencePercent,
                            row.GetIssues()))
                .ToArray();

        return Result.Success<
            BulkUpdateCatalogPriceListRowsResult,
            DomainError>(
                new BulkUpdateCatalogPriceListRowsResult(
                    priceList.Id,
                    priceList.Status,
                    priceList.RowsCount,
                    priceList.ValidRowsCount,
                    priceList.ErrorRowsCount,
                    resultRows));
    }
}