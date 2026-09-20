using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

public sealed class ApplyCatalogPriceCalculationImportCommandHandler
{
    private const int MaximumRowsCount = 5_000;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceCalculationRepository
        _calculationRepository;

    private readonly ICatalogActiveProductPriceReader
        _activeProductPriceReader;

    private readonly IUnitOfWork _unitOfWork;

    public ApplyCatalogPriceCalculationImportCommandHandler(
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
        ApplyCatalogPriceCalculationImportResult,
        DomainError>> Handle(
            ApplyCatalogPriceCalculationImportCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationResult = Validate(command);

        if (validationResult.IsFailure)
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(validationResult.Error);
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
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser.CanModifyOwnPriceCalculation())
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
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
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CalculationNotFound(
                            command.CalculationId));
        }

        if (calculation.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .UserCannotModifyCalculation());
        }

        if (!calculation.IsEditable)
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .CannotModifyCalculation(
                            calculation.Status));
        }

        var groupedRows =
            command.Rows
                .GroupBy(row => row.ProductId)
                .Select(
                    group =>
                        new ApplyCatalogPriceCalculationImportRow(
                            group.Key,
                            group.Sum(row => row.Quantity)))
                .ToArray();

        if (groupedRows.Any(
                row =>
                    row.Quantity
                    > CatalogPriceCalculationLine.MaximumQuantity))
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .QuantityIsTooLarge(
                            CatalogPriceCalculationLine
                                .MaximumQuantity));
        }

        var priceSources =
            await _activeProductPriceReader
                .FindByProductIdsAsync(
                    groupedRows
                        .Select(row => row.ProductId)
                        .ToArray(),
                    cancellationToken)
                .ConfigureAwait(false);

        var sourcesByProductId =
            priceSources
                .GroupBy(source => source.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray());

        var missingProductId =
            groupedRows
                .Select(row => row.ProductId)
                .FirstOrDefault(
                    productId =>
                        !sourcesByProductId.ContainsKey(
                            productId));

        if (missingProductId != Guid.Empty)
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .ActiveProductPriceNotFound(
                            missingProductId));
        }

        var ambiguousProductId =
            groupedRows
                .Select(row => row.ProductId)
                .FirstOrDefault(
                    productId =>
                        sourcesByProductId[productId]
                            .Length > 1);

        if (ambiguousProductId != Guid.Empty)
        {
            return Result.Failure<
                ApplyCatalogPriceCalculationImportResult,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .ActiveProductPriceIsAmbiguous(
                            ambiguousProductId));
        }

        var addedLinesCount = 0;

        foreach (var row in groupedRows)
        {
            var priceSource =
                sourcesByProductId[row.ProductId][0];

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
                    row.Quantity,
                    priceSource.BasePriceAmount,
                    priceSource.MrcPriceAmount);

            if (addLineResult.IsFailure)
            {
                return Result.Failure<
                    ApplyCatalogPriceCalculationImportResult,
                    DomainError>(addLineResult.Error);
            }

            addedLinesCount++;
        }

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            ApplyCatalogPriceCalculationImportResult,
            DomainError>(
                new ApplyCatalogPriceCalculationImportResult(
                    calculation.Id,
                    addedLinesCount,
                    calculation.TotalAmount));
    }

    private static UnitResult<DomainError> Validate(
        ApplyCatalogPriceCalculationImportCommand command)
    {
        if (command.CalculationId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.CalculationId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .CurrentUserNotFound());
        }

        if (command.Rows.Count is 0 or > MaximumRowsCount)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.Rows)));
        }

        if (command.Rows.Any(
                row => row.ProductId == Guid.Empty))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(ApplyCatalogPriceCalculationImportRow
                        .ProductId)));
        }

        if (command.Rows.Any(row => row.Quantity <= 0m))
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .QuantityMustBePositive());
        }

        if (command.Rows.Any(
                row =>
                    row.Quantity
                    > CatalogPriceCalculationLine.MaximumQuantity))
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .QuantityIsTooLarge(
                        CatalogPriceCalculationLine
                            .MaximumQuantity));
        }

        return UnitResult.Success<DomainError>();
    }
}
