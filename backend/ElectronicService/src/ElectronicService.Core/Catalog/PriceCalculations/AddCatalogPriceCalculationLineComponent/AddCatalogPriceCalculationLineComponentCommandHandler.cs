using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLineComponent;

public sealed class AddCatalogPriceCalculationLineComponentCommandHandler
{
    private const int MaximumPriceSourcesCount = 2;

    private readonly IUserRepository _userRepository;
    private readonly ICatalogPriceCalculationRepository _calculationRepository;
    private readonly ICatalogComponentCompatibilityService _componentCompatibilityService;
    private readonly ICatalogActiveProductPriceReader _activeProductPriceReader;
    private readonly IUnitOfWork _unitOfWork;

    public AddCatalogPriceCalculationLineComponentCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationRepository calculationRepository,
        ICatalogComponentCompatibilityService componentCompatibilityService,
        ICatalogActiveProductPriceReader activeProductPriceReader,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationRepository);
        ArgumentNullException.ThrowIfNull(componentCompatibilityService);
        ArgumentNullException.ThrowIfNull(activeProductPriceReader);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _userRepository = userRepository;
        _calculationRepository = calculationRepository;
        _componentCompatibilityService = componentCompatibilityService;
        _activeProductPriceReader = activeProductPriceReader;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddCatalogPriceCalculationLineComponentResult, DomainError>> Handle(
        AddCatalogPriceCalculationLineComponentCommand command,
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

        if (command.NeedDefinitionId == Guid.Empty)
        {
            return Failure(GeneralErrors.ValueIsInvalid(nameof(command.NeedDefinitionId)));
        }

        if (command.ComponentProductId == Guid.Empty)
        {
            return Failure(GeneralErrors.ValueIsInvalid(nameof(command.ComponentProductId)));
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
            return Failure(
                CatalogPriceCalculationErrors.CalculationNotFound(command.CalculationId));
        }

        if (calculation.CreatedByUserId != currentUser.Id)
        {
            return Failure(CatalogPriceCalculationErrors.UserCannotModifyCalculation());
        }

        if (!calculation.IsEditable)
        {
            return Failure(
                CatalogPriceCalculationErrors.CannotModifyCalculation(calculation.Status));
        }

        var line = calculation.Lines.SingleOrDefault(
            calculationLine => calculationLine.Id == command.LineId);

        if (line is null)
        {
            return Failure(CatalogPriceCalculationErrors.LineNotFound(command.LineId));
        }

        var compatibilityResult = await _componentCompatibilityService
            .GetCompatibilityAsync(line.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (compatibilityResult.IsFailure)
        {
            return Failure(compatibilityResult.Error);
        }

        var need = compatibilityResult.Value.Needs.SingleOrDefault(
            item => item.NeedDefinitionId == command.NeedDefinitionId);

        if (need is null)
        {
            return Failure(
                ComponentCompatibilityErrors.NeedDoesNotBelongToProductType());
        }

        var isCompatible = need.CompatibleComponents.Any(
            component => component.ProductId == command.ComponentProductId);

        if (!isCompatible)
        {
            return Failure(ComponentCompatibilityErrors.CompatibleOfferNotFound());
        }

        var priceSources = await _activeProductPriceReader
            .FindByProductIdAsync(
                command.ComponentProductId,
                MaximumPriceSourcesCount,
                cancellationToken)
            .ConfigureAwait(false);

        if (priceSources.Count == 0)
        {
            return Failure(
                CatalogPriceCalculationErrors.ActiveProductPriceNotFound(
                    command.ComponentProductId));
        }

        if (priceSources.Count > 1)
        {
            return Failure(
                CatalogPriceCalculationErrors.ActiveProductPriceIsAmbiguous(
                    command.ComponentProductId));
        }

        var priceSource = priceSources[0];

        var addResult = calculation.AddLineComponent(
            command.LineId,
            command.NeedDefinitionId,
            need.Name,
            priceSource.ProductId,
            priceSource.ManufacturerId,
            priceSource.Article,
            priceSource.Name,
            priceSource.ManufacturerName,
            command.QuantityPerUnit,
            priceSource.BasePriceAmount);

        if (addResult.IsFailure)
        {
            return Failure(addResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var componentLine = line.Components.Single(
            component => component.Id == addResult.Value);

        return new AddCatalogPriceCalculationLineComponentResult(
            calculation.Id,
            line.Id,
            componentLine.Id,
            componentLine.NeedDefinitionId,
            componentLine.NeedName,
            componentLine.ComponentProductId,
            componentLine.ManufacturerId,
            componentLine.ManufacturerName,
            componentLine.Article,
            componentLine.Name,
            componentLine.QuantityPerUnit,
            componentLine.BasePriceAmount,
            componentLine.DiscountPercent,
            componentLine.ProjectPriceAmount,
            componentLine.TotalQuantity,
            componentLine.TotalAmount,
            line.TotalAmount,
            calculation.TotalAmount);
    }

    private static Result<AddCatalogPriceCalculationLineComponentResult, DomainError> Failure(
        DomainError error)
    {
        return Result.Failure<AddCatalogPriceCalculationLineComponentResult, DomainError>(
            error);
    }
}