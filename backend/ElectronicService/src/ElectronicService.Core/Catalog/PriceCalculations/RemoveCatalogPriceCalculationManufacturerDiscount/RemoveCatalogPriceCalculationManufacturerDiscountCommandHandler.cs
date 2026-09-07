using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.Models;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationManufacturerDiscount;

public sealed class RemoveCatalogPriceCalculationManufacturerDiscountCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public RemoveCatalogPriceCalculationManufacturerDiscountCommandHandler(
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
        RemoveCatalogPriceCalculationManufacturerDiscountResult,
        DomainError>> Handle(
            RemoveCatalogPriceCalculationManufacturerDiscountCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.CalculationId)));
        }

        if (command.ManufacturerId == Guid.Empty)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.ManufacturerId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
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
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
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
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var manufacturerLinesBeforeRemoval =
            calculation.Lines
                .Where(
                    line =>
                        line.ManufacturerId
                        == command.ManufacturerId)
                .ToArray();

        var removeDiscountResult =
            calculation.RemoveManufacturerDiscount(
                command.ManufacturerId);

        if (removeDiscountResult.IsFailure)
        {
            return Result.Failure<
                RemoveCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    removeDiscountResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var lines =
            manufacturerLinesBeforeRemoval
                .OrderBy(line =>
                    line.CreatedAtUtc)
                .ThenBy(line =>
                    line.Id)
                .Select(
                    line =>
                        new CatalogPriceCalculationRecalculatedLine(
                            line.Id,
                            line.ProductId,
                            line.Quantity,
                            line.BasePriceAmount,
                            line.DiscountPercent,
                            line.ProjectPriceAmount,
                            line.TotalAmount))
                .ToArray();

        return Result.Success<
            RemoveCatalogPriceCalculationManufacturerDiscountResult,
            DomainError>(
                new RemoveCatalogPriceCalculationManufacturerDiscountResult(
                    calculation.Id,
                    command.ManufacturerId,
                    manufacturerLinesBeforeRemoval[0]
                        .ManufacturerName,
                    0m,
                    manufacturerLinesBeforeRemoval.Length,
                    calculation.TotalAmount,
                    lines));
    }
}