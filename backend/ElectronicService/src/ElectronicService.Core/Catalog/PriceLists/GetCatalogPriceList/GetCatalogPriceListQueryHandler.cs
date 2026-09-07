using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceList;

public sealed class GetCatalogPriceListQueryHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListReader _priceListReader;

    public GetCatalogPriceListQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceListReader priceListReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListReader);

        _userRepository = userRepository;
        _priceListReader = priceListReader;
    }

    public async Task<Result<
        CatalogPriceListDetails,
        DomainError>> Handle(
            GetCatalogPriceListQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var permissionResult =
            await EnsureCanViewAsync(
                    query.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceListDetails,
                DomainError>(
                    permissionResult.Error);
        }

        var priceList =
            await _priceListReader
                .GetByIdAsync(
                    query.PriceListId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (priceList is null)
        {
            return Result.Failure<
                CatalogPriceListDetails,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        query.PriceListId));
        }

        return Result.Success<
            CatalogPriceListDetails,
            DomainError>(priceList);
    }

    private async Task<UnitResult<DomainError>>
        EnsureCanViewAsync(
            Guid currentUserId,
            CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.CurrentUserNotFound());
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    currentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.UserCannotViewPriceList());
        }

        return UnitResult.Success<DomainError>();
    }
}