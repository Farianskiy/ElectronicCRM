using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLine;

public sealed class RemoveCatalogPriceCalculationLineCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public RemoveCatalogPriceCalculationLineCommandHandler(
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
        RemoveCatalogPriceCalculationLineResult,
        DomainError>> Handle(
            RemoveCatalogPriceCalculationLineCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationLineResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.CalculationId)));
        }

        if (command.LineId == Guid.Empty)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationLineResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.LineId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationLineResult,
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
                RemoveCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationLineResult,
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
                RemoveCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var removeResult =
            calculation.RemoveLine(
                command.LineId);

        if (removeResult.IsFailure)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationLineResult,
                DomainError>(
                    removeResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            RemoveCatalogPriceCalculationLineResult,
            DomainError>(
                new RemoveCatalogPriceCalculationLineResult(
                    calculation.Id,
                    command.LineId,
                    calculation.Lines.Count,
                    calculation.TotalAmount));
    }
}