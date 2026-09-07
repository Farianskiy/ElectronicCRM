using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.Models;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.SetCatalogPriceCalculationManufacturerDiscount;

public sealed class SetCatalogPriceCalculationManufacturerDiscountCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public SetCatalogPriceCalculationManufacturerDiscountCommandHandler(
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
        SetCatalogPriceCalculationManufacturerDiscountResult,
        DomainError>> Handle(
            SetCatalogPriceCalculationManufacturerDiscountCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CalculationId == Guid.Empty)
        {
            return Result.Failure<
                SetCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.CalculationId)));
        }

        if (command.ManufacturerId == Guid.Empty)
        {
            return Result.Failure<
                SetCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.ManufacturerId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                SetCatalogPriceCalculationManufacturerDiscountResult,
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
                SetCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                SetCatalogPriceCalculationManufacturerDiscountResult,
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
                SetCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                SetCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        var setDiscountResult =
            calculation.SetManufacturerDiscount(
                command.ManufacturerId,
                command.DiscountPercent);

        if (setDiscountResult.IsFailure)
        {
            return Result.Failure<
                SetCatalogPriceCalculationManufacturerDiscountResult,
                DomainError>(
                    setDiscountResult.Error);
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturerLines =
            calculation.Lines
                .Where(
                    line =>
                        line.ManufacturerId
                        == command.ManufacturerId)
                .OrderBy(line =>
                    line.CreatedAtUtc)
                .ThenBy(line =>
                    line.Id)
                .ToArray();

        var discount =
            calculation.ManufacturerDiscounts
                .Single(
                    existingDiscount =>
                        existingDiscount.ManufacturerId
                        == command.ManufacturerId);

        var lines =
            manufacturerLines
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
            SetCatalogPriceCalculationManufacturerDiscountResult,
            DomainError>(
                new SetCatalogPriceCalculationManufacturerDiscountResult(
                    calculation.Id,
                    command.ManufacturerId,
                    manufacturerLines[0].ManufacturerName,
                    discount.DiscountPercent,
                    manufacturerLines.Length,
                    calculation.TotalAmount,
                    lines));
    }
}