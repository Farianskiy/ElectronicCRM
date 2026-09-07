using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.CompleteCatalogPriceCalculation;

public sealed class CompleteCatalogPriceCalculationCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CompleteCatalogPriceCalculationCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationRepository calculationRepository,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _userRepository = userRepository;
        _calculationRepository = calculationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<
        CompleteCatalogPriceCalculationResult,
        DomainError>> Handle(
            CompleteCatalogPriceCalculationCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.CalculationId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
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
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var calculation =
            await _calculationRepository
                .GetByIdAsync(
                    command.CalculationId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (calculation is null)
        {
            return Result.Failure<
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var completeResult =
            calculation.Complete();

        if (completeResult.IsFailure)
        {
            return Result.Failure<
                CompleteCatalogPriceCalculationResult,
                DomainError>(
                    completeResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturersCount =
            calculation.Lines
                .Select(line =>
                    line.ManufacturerId)
                .Distinct()
                .Count();

        return Result.Success<
            CompleteCatalogPriceCalculationResult,
            DomainError>(
                new CompleteCatalogPriceCalculationResult(
                    calculation.Id,
                    calculation.Title,
                    calculation.Currency,
                    calculation.Status,
                    calculation.Lines.Count,
                    manufacturersCount,
                    calculation.TotalAmount,
                    calculation.CompletedAtUtc));
    }
}