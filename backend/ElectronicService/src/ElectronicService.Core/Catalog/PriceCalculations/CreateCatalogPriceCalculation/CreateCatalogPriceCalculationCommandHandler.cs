using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.CreateCatalogPriceCalculation;

public sealed class CreateCatalogPriceCalculationCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CreateCatalogPriceCalculationCommandHandler(
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
        CreateCatalogPriceCalculationResult,
        DomainError>> Handle(
            CreateCatalogPriceCalculationCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CreateCatalogPriceCalculationResult,
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
                CreateCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanCreatePriceCalculation())
        {
            return Result.Failure<
                CreateCatalogPriceCalculationResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotCreateCalculation());
        }

        var calculationResult =
            CatalogPriceCalculation.Create(
                currentUser.Id,
                command.Title);

        if (calculationResult.IsFailure)
        {
            return Result.Failure<
                CreateCatalogPriceCalculationResult,
                DomainError>(
                    calculationResult.Error);
        }

        var calculation =
            calculationResult.Value;

        _calculationRepository.Add(calculation);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            CreateCatalogPriceCalculationResult,
            DomainError>(
                new CreateCatalogPriceCalculationResult(
                    calculation.Id,
                    calculation.CreatedByUserId,
                    calculation.Title,
                    calculation.Currency,
                    calculation.Status,
                    calculation.TotalAmount,
                    calculation.CreatedAtUtc));
    }
}