using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLineComponent;

public sealed class RemoveCatalogPriceCalculationLineComponentCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ICatalogPriceCalculationRepository _calculationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCatalogPriceCalculationLineComponentCommandHandler(
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

    public async Task<Result<RemoveCatalogPriceCalculationLineComponentResult, DomainError>> Handle(
        RemoveCatalogPriceCalculationLineComponentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Failure(GeneralErrors.ValueIsInvalid(nameof(command.CalculationId)));
        }

        if (command.LineId == Guid.Empty)
        {
            return Failure(GeneralErrors.ValueIsInvalid(nameof(command.LineId)));
        }

        if (command.ComponentLineId == Guid.Empty)
        {
            return Failure(GeneralErrors.ValueIsInvalid(nameof(command.ComponentLineId)));
        }

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
            return Failure(CatalogPriceCalculationErrors.CalculationNotFound(command.CalculationId));
        }

        if (calculation.CreatedByUserId != currentUser.Id)
        {
            return Failure(CatalogPriceCalculationErrors.UserCannotModifyCalculation());
        }

        var removalResult = calculation.RemoveLineComponent(command.LineId, command.ComponentLineId);

        if (removalResult.IsFailure)
        {
            return Failure(removalResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var line = calculation.Lines.Single(item => item.Id == command.LineId);

        return new RemoveCatalogPriceCalculationLineComponentResult(
            calculation.Id,
            line.Id,
            command.ComponentLineId,
            line.Components.Count,
            line.TotalAmount,
            calculation.TotalAmount);
    }

    private static Result<RemoveCatalogPriceCalculationLineComponentResult, DomainError> Failure(
        DomainError error)
    {
        return Result.Failure<RemoveCatalogPriceCalculationLineComponentResult, DomainError>(error);
    }
}
