using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListRows;

public sealed class GetCatalogPriceListRowsQueryHandler
{
    public const int MaximumPageSize = 200;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListReader _priceListReader;

    public GetCatalogPriceListRowsQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceListReader priceListReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListReader);

        _userRepository = userRepository;
        _priceListReader = priceListReader;
    }

    public async Task<Result<
        CatalogPriceListRowsPage,
        DomainError>> Handle(
            GetCatalogPriceListRowsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListRowsPage,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (query.Page <= 0)
        {
            return Result.Failure<
                CatalogPriceListRowsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        if (query.PageSize <= 0
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogPriceListRowsPage,
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
                CatalogPriceListRowsPage,
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
                CatalogPriceListRowsPage,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                CatalogPriceListRowsPage,
                DomainError>(
                    CatalogPriceListErrors.UserCannotViewPriceList());
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
                CatalogPriceListRowsPage,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        query.PriceListId));
        }

        var rows =
            await _priceListReader
                .GetRowsAsync(
                    query.PriceListId,
                    query.Status,
                    query.MatchStatus,
                    query.IssueCode,
                    (int)skipValue,
                    query.PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceListRowsPage,
            DomainError>(rows);
    }
}