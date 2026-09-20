using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.UpdateCatalogPriceCalculationCard;

public sealed class UpdateCatalogPriceCalculationCardCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ICatalogPriceCalculationRepository _calculationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCatalogPriceCalculationCardCommandHandler(
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

    public async Task<Result<UpdateCatalogPriceCalculationCardResult, DomainError>> Handle(
        UpdateCatalogPriceCalculationCardCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(GeneralErrors.ValueIsInvalid(nameof(command.CalculationId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(CatalogPriceCalculationErrors.CurrentUserNotFound());
        }

        var currentUser = await _userRepository.GetByIdAsync(command.CurrentUserId, cancellationToken).ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(CatalogPriceCalculationErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(CatalogPriceCalculationErrors.UserCannotModifyCalculation());
        }

        var calculation = await _calculationRepository.GetByIdAsync(command.CalculationId, cancellationToken).ConfigureAwait(false);

        if (calculation is null)
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(CatalogPriceCalculationErrors.CalculationNotFound(command.CalculationId));
        }

        if (calculation.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(CatalogPriceCalculationErrors.UserCannotModifyCalculation());
        }

        var updateResult = calculation.UpdateProjectCard(command.CustomerName, command.ObjectName, command.ProjectNumber, command.ResponsibleName, command.Comment, command.ValidUntil);

        if (updateResult.IsFailure)
        {
            return Result.Failure<UpdateCatalogPriceCalculationCardResult, DomainError>(updateResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<UpdateCatalogPriceCalculationCardResult, DomainError>(
            new UpdateCatalogPriceCalculationCardResult(
                calculation.Id,
                calculation.CustomerName,
                calculation.ObjectName,
                calculation.ProjectNumber,
                calculation.ResponsibleName,
                calculation.Comment,
                calculation.ValidUntil,
                calculation.UpdatedAtUtc));
    }
}