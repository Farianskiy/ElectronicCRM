using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.GetCatalogPriceCalculation;

public sealed class GetCatalogPriceCalculationQueryHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationReader
        _calculationReader;

    public GetCatalogPriceCalculationQueryHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationReader calculationReader)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationReader);

        _userRepository = userRepository;
        _calculationReader = calculationReader;
    }

    public async Task<Result<
        CatalogPriceCalculationDetails,
        DomainError>> Handle(
            GetCatalogPriceCalculationQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationDetails,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.CalculationId)));
        }

        var permissionResult =
            await EnsureCanViewAsync(
                    query.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceCalculationDetails,
                DomainError>(
                    permissionResult.Error);
        }

        var calculation =
            await _calculationReader
                .GetByIdAsync(
                    query.CalculationId,
                    query.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (calculation is null)
        {
            return Result.Failure<
                CatalogPriceCalculationDetails,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            query.CalculationId));
        }

        return Result.Success<
            CatalogPriceCalculationDetails,
            DomainError>(calculation);
    }

    private async Task<UnitResult<DomainError>>
        EnsureCanViewAsync(
            Guid currentUserId,
            CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .CurrentUserNotFound());
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
                CatalogPriceCalculationErrors
                    .CurrentUserNotFound());
        }

        if (!currentUser.CanViewOwnPriceCalculations())
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .UserCannotViewCalculations());
        }

        return UnitResult.Success<DomainError>();
    }
}