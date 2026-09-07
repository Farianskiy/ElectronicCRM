using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListVersions;

public sealed class GetCatalogPriceListVersionsQueryHandler
{
    public const int MaximumPageSize = 100;

    private readonly IUserRepository _userRepository;

    private readonly IManufacturerRepository
        _manufacturerRepository;

    private readonly ICatalogPriceListReader
        _priceListReader;

    public GetCatalogPriceListVersionsQueryHandler(
        IUserRepository userRepository,
        IManufacturerRepository manufacturerRepository,
        ICatalogPriceListReader priceListReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(manufacturerRepository);
        ArgumentNullException.ThrowIfNull(priceListReader);

        _userRepository = userRepository;
        _manufacturerRepository = manufacturerRepository;
        _priceListReader = priceListReader;
    }

    public async Task<Result<
        CatalogPriceListVersionsPage,
        DomainError>> Handle(
            GetCatalogPriceListVersionsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ManufacturerId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.ManufacturerId)));
        }

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    CatalogPriceListErrors
                        .CurrentUserNotFound());
        }

        if (query.Page <= 0)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        if (query.PageSize <= 0
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.PageSize)));
        }

        if (query.Status
            == CatalogPriceListStatus.None)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Status)));
        }

        var skipValue =
            (long)(query.Page - 1)
            * query.PageSize;

        if (skipValue > int.MaxValue)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
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
                CatalogPriceListVersionsPage,
                DomainError>(
                    CatalogPriceListErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    CatalogPriceListErrors
                        .UserCannotViewPriceList());
        }

        var manufacturerExists =
            await _manufacturerRepository
                .ExistsByIdAsync(
                    query.ManufacturerId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (!manufacturerExists)
        {
            return Result.Failure<
                CatalogPriceListVersionsPage,
                DomainError>(
                    CatalogErrors.ManufacturerNotFound(
                        query.ManufacturerId));
        }

        var versions =
            await _priceListReader
                .GetVersionsAsync(
                    query.ManufacturerId,
                    query.Status,
                    (int)skipValue,
                    query.PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceListVersionsPage,
            DomainError>(versions);
    }
}