using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentSupplyStatus;

public sealed class ChangeCatalogPriceCalculationLineComponentSupplyStatusCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ICatalogPriceCalculationRepository _calculationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeCatalogPriceCalculationLineComponentSupplyStatusCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationRepository calculationRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _calculationRepository = calculationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<
        ChangeCatalogPriceCalculationLineComponentSupplyStatusResult,
        DomainError>> Handle(
            ChangeCatalogPriceCalculationLineComponentSupplyStatusCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Failure(CatalogPriceCalculationErrors.CurrentUserNotFound());
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Failure(CatalogPriceCalculationErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Failure(CatalogPriceCalculationErrors.UserCannotModifyCalculation());
        }

        var calculation = await _calculationRepository
            .GetByIdAsync(command.CalculationId, cancellationToken)
            .ConfigureAwait(false);

        if (calculation is null)
        {
            return Failure(
                CatalogPriceCalculationErrors.CalculationNotFound(
                    command.CalculationId));
        }

        if (calculation.CreatedByUserId != currentUser.Id)
        {
            return Failure(CatalogPriceCalculationErrors.UserCannotModifyCalculation());
        }

        var changeResult = calculation.ChangeLineComponentSupplyStatus(
            command.LineId,
            command.ComponentLineId,
            command.SupplyStatus);

        if (changeResult.IsFailure)
        {
            return Failure(changeResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var line = calculation.Lines.Single(item => item.Id == command.LineId);
        var component = line.Components.Single(
            item => item.Id == command.ComponentLineId);

        return new ChangeCatalogPriceCalculationLineComponentSupplyStatusResult(
            calculation.Id,
            line.Id,
            component.Id,
            component.SupplyStatus,
            component.TotalAmount,
            line.TotalAmount,
            calculation.TotalAmount);
    }

    private static Result<
        ChangeCatalogPriceCalculationLineComponentSupplyStatusResult,
        DomainError> Failure(DomainError error)
    {
        return Result.Failure<
            ChangeCatalogPriceCalculationLineComponentSupplyStatusResult,
            DomainError>(error);
    }
}
