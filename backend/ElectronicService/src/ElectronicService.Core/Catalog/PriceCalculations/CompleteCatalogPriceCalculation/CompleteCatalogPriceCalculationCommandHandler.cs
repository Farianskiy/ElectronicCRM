using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.CompleteCatalogPriceCalculation;

public sealed class CompleteCatalogPriceCalculationCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly ICatalogComponentCompatibilityService
        _componentCompatibilityService;

    private readonly IUnitOfWork _unitOfWork;

    public CompleteCatalogPriceCalculationCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationRepository calculationRepository,
        ICatalogComponentCompatibilityService componentCompatibilityService,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationRepository);
        ArgumentNullException.ThrowIfNull(componentCompatibilityService);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _userRepository = userRepository;
        _calculationRepository = calculationRepository;
        _componentCompatibilityService = componentCompatibilityService;
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

        foreach (var line in calculation.Lines)
        {
            var compatibilityResult =
                await _componentCompatibilityService
                    .GetCompatibilityAsync(
                        line.ProductId,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (compatibilityResult.IsFailure)
            {
                return Result.Failure<
                    CompleteCatalogPriceCalculationResult,
                    DomainError>(compatibilityResult.Error);
            }

            var selectedNeedIds = line.Components
                .Select(component => component.NeedDefinitionId)
                .ToHashSet();
            var unresolvedRequiredNeeds = compatibilityResult.Value.Needs
                .Where(need =>
                    need.RequirementMode
                        == ComponentNeedRequirementMode.RequiredForAssembly
                    && !selectedNeedIds.Contains(need.NeedDefinitionId))
                .Select(need => need.Name)
                .ToArray();

            if (unresolvedRequiredNeeds.Length > 0)
            {
                return Result.Failure<
                    CompleteCatalogPriceCalculationResult,
                    DomainError>(
                        CatalogPriceCalculationErrors
                            .RequiredComponentDecisionsMissing(
                                line.Article,
                                unresolvedRequiredNeeds));
            }
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
