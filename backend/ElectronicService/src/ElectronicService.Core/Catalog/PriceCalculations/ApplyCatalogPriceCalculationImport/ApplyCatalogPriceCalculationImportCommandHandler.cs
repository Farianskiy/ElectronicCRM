using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Catalog.Products;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Components;
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

    private readonly ICatalogComponentCompatibilityService
        _componentCompatibilityService;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IProductRepository _productRepository;
    private readonly ICatalogProductMetadataRepository _productMetadataRepository;
    private readonly IProductAuditRecorder _productAuditRecorder;

    public ApplyCatalogPriceCalculationImportCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceCalculationRepository calculationRepository,
        ICatalogActiveProductPriceReader activeProductPriceReader,
        ICatalogComponentCompatibilityService componentCompatibilityService,
        IProductRepository productRepository,
        ICatalogProductMetadataRepository productMetadataRepository,
        IProductAuditRecorder productAuditRecorder,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(calculationRepository);
        ArgumentNullException.ThrowIfNull(activeProductPriceReader);
        ArgumentNullException.ThrowIfNull(componentCompatibilityService);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(productMetadataRepository);
        ArgumentNullException.ThrowIfNull(productAuditRecorder);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _userRepository = userRepository;
        _calculationRepository = calculationRepository;
        _activeProductPriceReader =
            activeProductPriceReader;
        _componentCompatibilityService = componentCompatibilityService;
        _productRepository = productRepository;
        _productMetadataRepository = productMetadataRepository;
        _productAuditRecorder = productAuditRecorder;
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

        var addRows = command.Rows
            .Where(row => row.Action == CatalogPriceCalculationImportAction.Add)
            .GroupBy(row => row.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(row => row.Quantity!.Value)
            })
            .ToArray();

        if (addRows.Any(row =>
                row.Quantity > CatalogPriceCalculationLine.MaximumQuantity))
        {
            return Failure(
                CatalogPriceCalculationErrors.QuantityIsTooLarge(
                    CatalogPriceCalculationLine.MaximumQuantity));
        }

        var priceSources =
            await _activeProductPriceReader
                .FindByProductIdsAsync(
                    addRows
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
            addRows
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
                        .CatalogProductNotFound(
                            missingProductId));
        }

        var addedLinesCount = 0;
        var updatedLinesCount = 0;
        var removedLinesCount = 0;
        var addedComponentsCount = 0;
        var updatedComponentsCount = 0;
        var removedComponentsCount = 0;
        var updatedCharacteristicsCount = 0;
        var removedCharacteristicsCount = 0;

        var allowedProductIds = calculation.Lines
            .Select(line => line.ProductId)
            .Concat(calculation.Lines.SelectMany(line =>
                line.Components.Select(component => component.ComponentProductId)))
            .ToHashSet();
        var characteristicProductIds = command.CharacteristicRows
            .Select(row => row.ProductId)
            .Distinct()
            .ToArray();

        if (characteristicProductIds.Any(productId =>
                !allowedProductIds.Contains(productId)))
        {
            return Failure(GeneralErrors.ValueIsInvalid(
                nameof(command.CharacteristicRows)));
        }

        var characteristicProducts = characteristicProductIds.Length == 0
            ? []
            : await _productRepository
                .GetByIdsWithDetailsAsync(
                    characteristicProductIds,
                    cancellationToken)
                .ConfigureAwait(false);
        var characteristicProductsById = characteristicProducts
            .ToDictionary(product => product.Id);

        if (characteristicProductsById.Count != characteristicProductIds.Length)
        {
            var missingCharacteristicProductId = characteristicProductIds.First(productId =>
                !characteristicProductsById.ContainsKey(productId));
            return Failure(CatalogPriceCalculationErrors.CatalogProductNotFound(
                missingCharacteristicProductId));
        }

        var beforeSnapshotsResult = await _productAuditRecorder
            .CaptureManyAsync(characteristicProducts, cancellationToken)
            .ConfigureAwait(false);

        if (beforeSnapshotsResult.IsFailure)
        {
            return Failure(beforeSnapshotsResult.Error);
        }

        foreach (var row in command.CharacteristicRows)
        {
            var product = characteristicProductsById[row.ProductId];
            var productType = await _productMetadataRepository
                .GetProductTypeByIdAsync(
                    product.ProductTypeId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (productType is null)
            {
                return Failure(CatalogPriceCalculationErrors.CatalogProductNotFound(
                    product.Id));
            }

            var definition = await _productMetadataRepository
                .GetCharacteristicDefinitionByCodeAsync(
                    row.CharacteristicCode,
                    cancellationToken)
                .ConfigureAwait(false);

            if (definition is null)
            {
                return Failure(GeneralErrors.ValueIsInvalid(
                    nameof(row.CharacteristicCode)));
            }

            if (row.Action == CatalogPriceCalculationCharacteristicImportAction.Remove)
            {
                var removeResult = product.RemoveCharacteristic(
                    productType,
                    definition.Id);

                if (removeResult.IsFailure)
                {
                    return Failure(removeResult.Error);
                }

                removedCharacteristicsCount++;
                continue;
            }

            var valueResult = ProductCharacteristicValueFactory.Create(
                definition.Code,
                definition.DataType,
                row.Value!);

            if (valueResult.IsFailure)
            {
                return Failure(valueResult.Error);
            }

            var setResult = product.SetCharacteristic(
                productType,
                definition,
                valueResult.Value);

            if (setResult.IsFailure)
            {
                return Failure(setResult.Error);
            }

            updatedCharacteristicsCount++;
        }

        foreach (var product in characteristicProducts)
        {
            var auditResult = await _productAuditRecorder
                .RecordManualChangeAsync(
                    product,
                    command.CurrentUserId,
                    ProductAuditOperation.ImportApplied,
                    beforeSnapshotsResult.Value[product.Id],
                    cancellationToken)
                .ConfigureAwait(false);

            if (auditResult.IsFailure)
            {
                return Failure(auditResult.Error);
            }
        }

        foreach (var row in command.ComponentRows.Where(row =>
                     row.Action == CatalogPriceCalculationImportAction.Remove))
        {
            var removeResult = calculation.RemoveLineComponent(
                row.MainLineId,
                row.ExistingComponentLineId!.Value);

            if (removeResult.IsFailure)
            {
                return Failure(removeResult.Error);
            }

            removedComponentsCount++;
        }

        foreach (var row in command.ComponentRows.Where(row =>
                     row.Action == CatalogPriceCalculationImportAction.UpdateQuantity))
        {
            var updateResult = calculation.ChangeLineComponentQuantityPerUnit(
                row.MainLineId,
                row.ExistingComponentLineId!.Value,
                row.QuantityPerUnit!.Value);

            if (updateResult.IsFailure)
            {
                return Failure(updateResult.Error);
            }

            updatedComponentsCount++;
        }

        foreach (var row in command.ComponentRows.Where(row =>
                     row.Action == CatalogPriceCalculationImportAction.Add))
        {
            var line = calculation.Lines.SingleOrDefault(
                item => item.Id == row.MainLineId);

            if (line is null)
            {
                return Failure(
                    CatalogPriceCalculationErrors.LineNotFound(row.MainLineId));
            }

            var compatibilityResult = await _componentCompatibilityService
                .GetCompatibilityAsync(line.ProductId, cancellationToken)
                .ConfigureAwait(false);

            if (compatibilityResult.IsFailure)
            {
                return Failure(compatibilityResult.Error);
            }

            var need = compatibilityResult.Value.Needs.SingleOrDefault(
                item => item.NeedDefinitionId == row.NeedDefinitionId);

            if (need is null)
            {
                return Failure(
                    ComponentCompatibilityErrors.NeedDoesNotBelongToProductType());
            }

            var manualValidationResult = await _componentCompatibilityService
                .ValidateManualSelectionAsync(
                    line.ProductId,
                    row.NeedDefinitionId,
                    row.ComponentProductId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (manualValidationResult.IsFailure)
            {
                return Failure(manualValidationResult.Error);
            }

            var componentPriceSources = await _activeProductPriceReader
                .FindByProductIdAsync(
                    row.ComponentProductId,
                    2,
                    cancellationToken)
                .ConfigureAwait(false);

            if (componentPriceSources.Count == 0)
            {
                return Failure(
                    CatalogPriceCalculationErrors.CatalogProductNotFound(
                        row.ComponentProductId));
            }

            var componentPriceSource = componentPriceSources[0];
            var addComponentResult = calculation.AddLineComponent(
                row.MainLineId,
                row.NeedDefinitionId,
                need.Name,
                componentPriceSource.ProductId,
                componentPriceSource.ManufacturerId,
                componentPriceSource.Article,
                componentPriceSource.Name,
                componentPriceSource.ManufacturerName,
                CatalogPriceCalculationLineComponentSelectionSource.Manual,
                row.QuantityPerUnit!.Value,
                componentPriceSource.BasePriceAmount);

            if (addComponentResult.IsFailure)
            {
                return Failure(addComponentResult.Error);
            }

            addedComponentsCount++;
        }

        foreach (var row in command.Rows.Where(row =>
                     row.Action == CatalogPriceCalculationImportAction.Remove))
        {
            var removeResult = calculation.RemoveLine(row.ExistingLineId!.Value);

            if (removeResult.IsFailure)
            {
                return Failure(removeResult.Error);
            }

            removedLinesCount++;
        }

        foreach (var row in command.Rows.Where(row =>
                     row.Action == CatalogPriceCalculationImportAction.UpdateQuantity))
        {
            var updateResult = calculation.ChangeLineQuantity(
                row.ExistingLineId!.Value,
                row.Quantity!.Value);

            if (updateResult.IsFailure)
            {
                return Failure(updateResult.Error);
            }

            updatedLinesCount++;
        }

        foreach (var row in addRows)
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
                    updatedLinesCount,
                    removedLinesCount,
                    addedComponentsCount,
                    updatedComponentsCount,
                    removedComponentsCount,
                    updatedCharacteristicsCount,
                    removedCharacteristicsCount,
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

        if (command.Rows.Count
                + command.ComponentRows.Count
                + command.CharacteristicRows.Count
            is 0 or > MaximumRowsCount)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command)));
        }

        if (command.Rows.Any(row =>
                row.ProductId == Guid.Empty))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(ApplyCatalogPriceCalculationImportRow
                        .ProductId)));
        }

        if (command.Rows.Any(row =>
                row.Action is CatalogPriceCalculationImportAction.Add
                    or CatalogPriceCalculationImportAction.UpdateQuantity
                && (!row.Quantity.HasValue || row.Quantity.Value <= 0m)))
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .QuantityMustBePositive());
        }

        if (command.Rows.Any(
                row =>
                    row.Quantity.HasValue
                    && row.Quantity.Value
                    > CatalogPriceCalculationLine.MaximumQuantity))
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .QuantityIsTooLarge(
                        CatalogPriceCalculationLine
                            .MaximumQuantity));
        }

        if (command.Rows.Any(row =>
                row.Action is CatalogPriceCalculationImportAction.UpdateQuantity
                    or CatalogPriceCalculationImportAction.Remove
                && (!row.ExistingLineId.HasValue
                    || row.ExistingLineId.Value == Guid.Empty)))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(ApplyCatalogPriceCalculationImportRow.ExistingLineId)));
        }

        var repeatedLineId = command.Rows
            .Where(row => row.ExistingLineId.HasValue)
            .GroupBy(row => row.ExistingLineId!.Value)
            .Any(group => group.Count() > 1);

        if (repeatedLineId)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.Rows)));
        }

        if (command.ComponentRows.Any(row =>
                row.MainLineId == Guid.Empty
                || row.NeedDefinitionId == Guid.Empty
                || row.ComponentProductId == Guid.Empty))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ComponentRows)));
        }

        if (command.ComponentRows.Any(row =>
                row.Action is CatalogPriceCalculationImportAction.Add
                    or CatalogPriceCalculationImportAction.UpdateQuantity
                && (!row.QuantityPerUnit.HasValue
                    || row.QuantityPerUnit.Value <= 0)))
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors.QuantityMustBePositive());
        }

        if (command.ComponentRows.Any(row =>
                row.QuantityPerUnit.HasValue
                && row.QuantityPerUnit.Value
                > CatalogPriceCalculationLineComponent.MaximumQuantityPerUnit))
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors.QuantityIsTooLarge(
                    CatalogPriceCalculationLineComponent.MaximumQuantityPerUnit));
        }

        if (command.ComponentRows.Any(row =>
                row.Action is CatalogPriceCalculationImportAction.UpdateQuantity
                    or CatalogPriceCalculationImportAction.Remove
                && (!row.ExistingComponentLineId.HasValue
                    || row.ExistingComponentLineId.Value == Guid.Empty)))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(ApplyCatalogPriceCalculationComponentImportRow
                        .ExistingComponentLineId)));
        }

        var repeatedComponentLineId = command.ComponentRows
            .Where(row => row.ExistingComponentLineId.HasValue)
            .GroupBy(row => row.ExistingComponentLineId!.Value)
            .Any(group => group.Count() > 1);

        if (repeatedComponentLineId)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.ComponentRows)));
        }

        if (command.CharacteristicRows.Any(row =>
                row.ProductId == Guid.Empty
                || string.IsNullOrWhiteSpace(row.CharacteristicCode)
                || row.Action == CatalogPriceCalculationCharacteristicImportAction.Set
                && string.IsNullOrWhiteSpace(row.Value)))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(
                nameof(command.CharacteristicRows)));
        }

        var repeatedCharacteristic = command.CharacteristicRows
            .GroupBy(row => (
                row.ProductId,
                row.CharacteristicCode.Trim().ToUpperInvariant()))
            .Any(group => group.Count() > 1);

        if (repeatedCharacteristic)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(
                nameof(command.CharacteristicRows)));
        }

        return UnitResult.Success<DomainError>();
    }

    private static Result<ApplyCatalogPriceCalculationImportResult, DomainError>
        Failure(DomainError error)
    {
        return Result.Failure<ApplyCatalogPriceCalculationImportResult, DomainError>(
            error);
    }
}
