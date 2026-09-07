using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListIssueGroups;

public sealed class GetCatalogPriceListIssueGroupsQueryHandler
{
    public const int MaximumPageSize = 100;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListReader _priceListReader;

    public GetCatalogPriceListIssueGroupsQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceListReader priceListReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListReader);

        _userRepository = userRepository;
        _priceListReader = priceListReader;
    }

    public async Task<Result<
        CatalogPriceListIssueGroupsPage,
        DomainError>> Handle(
            GetCatalogPriceListIssueGroupsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListIssueGroupsPage,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (query.PriceListId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListIssueGroupsPage,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        query.PriceListId));
        }

        if (query.Page <= 0)
        {
            return Result.Failure<
                CatalogPriceListIssueGroupsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        if (query.PageSize <= 0
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogPriceListIssueGroupsPage,
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
                CatalogPriceListIssueGroupsPage,
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
                CatalogPriceListIssueGroupsPage,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                CatalogPriceListIssueGroupsPage,
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
                CatalogPriceListIssueGroupsPage,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        query.PriceListId));
        }

        var groups =
            await _priceListReader
                .GetIssueGroupsAsync(
                    query.PriceListId,
                    query.IssueCode,
                    (int)skipValue,
                    query.PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceListIssueGroupsPage,
            DomainError>(groups);
    }
}