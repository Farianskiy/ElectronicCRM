using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.SearchCatalogPriceListProducts;

public sealed class SearchCatalogPriceListProductsQueryHandler
{
    public const int MaximumPageSize = 50;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListReader
        _priceListReader;

    public SearchCatalogPriceListProductsQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceListReader priceListReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListReader);

        _userRepository = userRepository;
        _priceListReader = priceListReader;
    }

    public async Task<Result<
        CatalogPriceListProductsPage,
        DomainError>> Handle(
            SearchCatalogPriceListProductsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListProductsPage,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (query.Page <= 0)
        {
            return Result.Failure<
                CatalogPriceListProductsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        if (query.PageSize <= 0
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogPriceListProductsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.PageSize)));
        }

        var skipValue =
            (long)(query.Page - 1)
            * query.PageSize;

        if (skipValue > int.MaxValue)
        {
            return Result.Failure<
                CatalogPriceListProductsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    query.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                CatalogPriceListProductsPage,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                CatalogPriceListProductsPage,
                DomainError>(
                    CatalogPriceListErrors
                        .UserCannotViewPriceList());
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
                CatalogPriceListProductsPage,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        query.PriceListId));
        }

        var products =
            await _priceListReader
                .SearchProductsAsync(
                    priceList.ManufacturerId,
                    query.Search,
                    (int)skipValue,
                    query.PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceListProductsPage,
            DomainError>(products);
    }
}