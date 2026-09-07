using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineQuantity;

public sealed class ChangeCatalogPriceCalculationLineQuantityCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public ChangeCatalogPriceCalculationLineQuantityCommandHandler(
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
        ChangeCatalogPriceCalculationLineQuantityResult,
        DomainError>> Handle(
            ChangeCatalogPriceCalculationLineQuantityCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                ChangeCatalogPriceCalculationLineQuantityResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.CalculationId)));
        }

        if (command.LineId == Guid.Empty)
        {
            return Result.Failure<
                ChangeCatalogPriceCalculationLineQuantityResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.LineId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                ChangeCatalogPriceCalculationLineQuantityResult,
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
                ChangeCatalogPriceCalculationLineQuantityResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                ChangeCatalogPriceCalculationLineQuantityResult,
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
                ChangeCatalogPriceCalculationLineQuantityResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                ChangeCatalogPriceCalculationLineQuantityResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var changeResult =
            calculation.ChangeLineQuantity(
                command.LineId,
                command.Quantity);

        if (changeResult.IsFailure)
        {
            return Result.Failure<
                ChangeCatalogPriceCalculationLineQuantityResult,
                DomainError>(
                    changeResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var line =
            calculation.Lines.Single(
                calculationLine =>
                    calculationLine.Id
                    == command.LineId);

        return Result.Success<
            ChangeCatalogPriceCalculationLineQuantityResult,
            DomainError>(
                new ChangeCatalogPriceCalculationLineQuantityResult(
                    calculation.Id,
                    line.Id,
                    line.Quantity,
                    line.BasePriceAmount,
                    line.DiscountPercent,
                    line.ProjectPriceAmount,
                    line.TotalAmount,
                    calculation.TotalAmount));
    }
}