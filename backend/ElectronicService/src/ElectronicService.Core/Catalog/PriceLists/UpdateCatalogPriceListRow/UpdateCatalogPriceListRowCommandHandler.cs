using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.UpdateCatalogPriceListRow;

public sealed class UpdateCatalogPriceListRowCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListRepository
        _priceListRepository;

    public UpdateCatalogPriceListRowCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceListRepository priceListRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListRepository);

        _userRepository = userRepository;
        _priceListRepository = priceListRepository;
    }

    public async Task<Result<
        UpdateCatalogPriceListRowResult,
        DomainError>> Handle(
            UpdateCatalogPriceListRowCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
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
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    CatalogPriceListErrors.UserCannotEditPriceList());
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
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        command.PriceListId));
        }

        if (priceList.Status
            is not CatalogPriceListStatus.NeedsCorrection
            and not CatalogPriceListStatus.Ready)
        {
            return Result.Failure<
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    CatalogPriceListErrors.RowsCannotBeEdited(
                        priceList.Status));
        }

        var row =
            await _priceListRepository
                .GetRowByIdAsync(
                    priceList.Id,
                    command.RowId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (row is null)
        {
            return Result.Failure<
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListRowNotFound(
                        command.RowId));
        }

        if (command.ProductId.HasValue)
        {
            var productBelongsToManufacturer =
                await _priceListRepository
                    .ProductBelongsToManufacturerAsync(
                        command.ProductId.Value,
                        priceList.ManufacturerId,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!productBelongsToManufacturer)
            {
                return Result.Failure<
                    UpdateCatalogPriceListRowResult,
                    DomainError>(
                        CatalogPriceListErrors
                            .ProductDoesNotBelongToManufacturer(
                                command.ProductId.Value,
                                priceList.ManufacturerId));
            }
        }

        var correctionResult =
            row.ApplyCorrection(
                command.Article,
                command.Name,
                command.BasePriceAmount,
                command.MrcPriceAmount,
                command.ProductUrl?.OriginalString,
                command.Unit,
                command.ProductId);

        if (correctionResult.IsFailure)
        {
            return Result.Failure<
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    correctionResult.Error);
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
                UpdateCatalogPriceListRowResult,
                DomainError>(
                    saveResult.Error);
        }

        var rowDetails =
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
                row.GetIssues());

        return Result.Success<
            UpdateCatalogPriceListRowResult,
            DomainError>(
                new UpdateCatalogPriceListRowResult(
                    priceList.Id,
                    priceList.Status,
                    priceList.RowsCount,
                    priceList.ValidRowsCount,
                    priceList.ErrorRowsCount,
                    rowDetails));
    }
}