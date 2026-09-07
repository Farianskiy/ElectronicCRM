using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLine;

public sealed class AddCatalogPriceCalculationLineCommandHandler
{
    private const int MaximumPriceSourcesCount = 2;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly ICatalogActiveProductPriceReader
        _activeProductPriceReader;

    private readonly IUnitOfWork _unitOfWork;

    public AddCatalogPriceCalculationLineCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationRepository calculationRepository,
        ICatalogActiveProductPriceReader activeProductPriceReader,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationRepository);
        ArgumentNullException.ThrowIfNull(activeProductPriceReader);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _userRepository = userRepository;
        _calculationRepository = calculationRepository;
        _activeProductPriceReader =
            activeProductPriceReader;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<
        AddCatalogPriceCalculationLineResult,
        DomainError>> Handle(
            AddCatalogPriceCalculationLineCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.CalculationId)));
        }

        if (command.ProductId == Guid.Empty)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.ProductId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
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
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
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
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        if (!calculation.IsEditable)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CannotModifyCalculation(
                            calculation.Status));
        }

        var priceSources =
            await _activeProductPriceReader
                .FindByProductIdAsync(
                    command.ProductId,
                    MaximumPriceSourcesCount,
                    cancellationToken)
                .ConfigureAwait(false);

        if (priceSources.Count == 0)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .ActiveProductPriceNotFound(
                            command.ProductId));
        }

        if (priceSources.Count > 1)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .ActiveProductPriceIsAmbiguous(
                            command.ProductId));
        }

        var priceSource =
            priceSources[0];

        var addLineResult =
            calculation.AddLine(
                priceSource.ProductId,
                priceSource.ManufacturerId,
                priceSource.PriceListId,
                priceSource.PriceListRowId,
                priceSource.Article,
                priceSource.Name,
                priceSource.ManufacturerName,
                priceSource.Unit,
                command.Quantity,
                priceSource.BasePriceAmount,
                priceSource.MrcPriceAmount);

        if (addLineResult.IsFailure)
        {
            return Result.Failure<
                AddCatalogPriceCalculationLineResult,
                DomainError>(
                    addLineResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var line =
            calculation.Lines.Single(
                calculationLine =>
                    calculationLine.Id
                    == addLineResult.Value);

        return Result.Success<
            AddCatalogPriceCalculationLineResult,
            DomainError>(
                new AddCatalogPriceCalculationLineResult(
                    calculation.Id,
                    line.Id,
                    line.ProductId,
                    line.ManufacturerId,
                    line.ManufacturerName,
                    line.PriceListId,
                    line.PriceListRowId,
                    line.Article,
                    line.Name,
                    line.Unit,
                    line.Quantity,
                    line.BasePriceAmount,
                    line.MrcPriceAmount,
                    line.DiscountPercent,
                    line.ProjectPriceAmount,
                    line.TotalAmount,
                    calculation.TotalAmount));
    }
}