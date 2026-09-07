using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.ActivateCatalogPriceList;

public sealed class ActivateCatalogPriceListCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListRepository
        _priceListRepository;

    public ActivateCatalogPriceListCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceListRepository priceListRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListRepository);

        _userRepository = userRepository;
        _priceListRepository = priceListRepository;
    }

    public async Task<Result<
        ActivateCatalogPriceListResult,
        DomainError>> Handle(
            ActivateCatalogPriceListCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PriceListId == Guid.Empty)
        {
            return Result.Failure<
                ActivateCatalogPriceListResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.PriceListId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                ActivateCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors
                        .CurrentUserNotFound());
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
                ActivateCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                ActivateCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors
                        .UserCannotActivatePriceList());
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
                ActivateCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        command.PriceListId));
        }

        if (priceList.Status
            != CatalogPriceListStatus.Ready)
        {
            return Result.Failure<
                ActivateCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors
                        .InvalidStatusTransition(
                            priceList.Status,
                            CatalogPriceListStatus.Active));
        }

        var activationResult =
            await _priceListRepository
                .ActivateAsync(
                    priceList,
                    cancellationToken)
                .ConfigureAwait(false);

        if (activationResult.IsFailure)
        {
            return Result.Failure<
                ActivateCatalogPriceListResult,
                DomainError>(
                    activationResult.Error);
        }

        return Result.Success<
            ActivateCatalogPriceListResult,
            DomainError>(
                new ActivateCatalogPriceListResult(
                    priceList.Id,
                    priceList.ManufacturerId,
                    priceList.Status,
                    priceList.ActivatedAtUtc,
                    activationResult.Value));
    }
}