using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.ExcludeCatalogPriceListUnmatchedRows;

public sealed class ExcludeCatalogPriceListUnmatchedRowsCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ICatalogPriceListRepository _priceListRepository;

    public ExcludeCatalogPriceListUnmatchedRowsCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceListRepository priceListRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListRepository);

        _userRepository = userRepository;
        _priceListRepository = priceListRepository;
    }

    public async Task<Result<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>>
        Handle(
            ExcludeCatalogPriceListUnmatchedRowsCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                CatalogPriceListErrors.CurrentUserNotFound());
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(command.CurrentUserId, cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                CatalogPriceListErrors.UserCannotEditPriceList());
        }

        var priceList =
            await _priceListRepository
                .GetByIdAsync(command.PriceListId, cancellationToken)
                .ConfigureAwait(false);

        if (priceList is null)
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                CatalogPriceListErrors.PriceListNotFound(command.PriceListId));
        }

        if (priceList.Status
            is not CatalogPriceListStatus.NeedsCorrection
            and not CatalogPriceListStatus.Ready)
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                CatalogPriceListErrors.RowsCannotBeEdited(priceList.Status));
        }

        if (priceList.ValidRowsCount == 0)
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                CatalogPriceListErrors.MatchedRowsRequiredForExclusion());
        }

        var exclusionResult =
            await _priceListRepository
                .ExcludeProductNotFoundRowsAndRefreshStatisticsAsync(
                    priceList,
                    cancellationToken)
                .ConfigureAwait(false);

        if (exclusionResult.IsFailure)
        {
            return Result.Failure<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
                exclusionResult.Error);
        }

        return Result.Success<ExcludeCatalogPriceListUnmatchedRowsResult, DomainError>(
            new ExcludeCatalogPriceListUnmatchedRowsResult(
                priceList.Id,
                priceList.Status,
                exclusionResult.Value,
                priceList.RowsCount,
                priceList.ValidRowsCount,
                priceList.ErrorRowsCount));
    }
}
