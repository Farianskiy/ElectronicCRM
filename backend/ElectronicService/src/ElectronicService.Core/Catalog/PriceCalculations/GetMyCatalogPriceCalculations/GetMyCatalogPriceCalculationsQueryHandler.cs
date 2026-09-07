using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.GetMyCatalogPriceCalculations;

public sealed class GetMyCatalogPriceCalculationsQueryHandler
{
    public const int MaximumPageSize = 100;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationReader
        _calculationReader;

    public GetMyCatalogPriceCalculationsQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationReader calculationReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationReader);

        _userRepository = userRepository;
        _calculationReader = calculationReader;
    }

    public async Task<Result<
        CatalogPriceCalculationsPage,
        DomainError>> Handle(
            GetMyCatalogPriceCalculationsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (query.Page <= 0)
        {
            return Result.Failure<
                CatalogPriceCalculationsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.Page)));
        }

        if (query.PageSize <= 0
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogPriceCalculationsPage,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.PageSize)));
        }

        if (query.Status
            == CatalogPriceCalculationStatus.None)
        {
            return Result.Failure<
                CatalogPriceCalculationsPage,
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
                CatalogPriceCalculationsPage,
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
                CatalogPriceCalculationsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanViewOwnPriceCalculations())
        {
            return Result.Failure<
                CatalogPriceCalculationsPage,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotViewCalculations());
        }

        var calculations =
            await _calculationReader
                .GetOwnAsync(
                    query.CurrentUserId,
                    query.Status,
                    (int)skipValue,
                    query.PageSize,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceCalculationsPage,
            DomainError>(calculations);
    }
}