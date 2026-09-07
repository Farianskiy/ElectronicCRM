using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.SearchCatalogPriceCalculationProducts;

public sealed class SearchCatalogPriceCalculationProductsQueryHandler
{
    public const int MaximumPageSize = 50;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationReader
        _calculationReader;

    private readonly ICatalogPriceCalculationProductSearchReader
        _productSearchReader;

    public SearchCatalogPriceCalculationProductsQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationReader calculationReader,
        ICatalogPriceCalculationProductSearchReader
            productSearchReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationReader);
        ArgumentNullException.ThrowIfNull(productSearchReader);

        _userRepository = userRepository;
        _calculationReader = calculationReader;
        _productSearchReader = productSearchReader;
    }

    public async Task<Result<
        CatalogPriceCalculationProductsPage,
        DomainError>> Handle(
            SearchCatalogPriceCalculationProductsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.CalculationId)));
        }

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (query.Page <= 0)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        if (query.PageSize <= 0
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
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
                CatalogPriceCalculationProductsPage,
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
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var calculation =
            await _calculationReader
                .GetAccessAsync(
                    query.CalculationId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (calculation is null)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            query.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        if (calculation.Status
            != CatalogPriceCalculationStatus.Draft)
        {
            return Result.Failure<
                CatalogPriceCalculationProductsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CannotModifyCalculation(
                            calculation.Status));
        }

        var products =
            await _productSearchReader
                .SearchAsync(
                    query.Search,
                    (int)skipValue,
                    query.PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceCalculationProductsPage,
            DomainError>(products);
    }
}