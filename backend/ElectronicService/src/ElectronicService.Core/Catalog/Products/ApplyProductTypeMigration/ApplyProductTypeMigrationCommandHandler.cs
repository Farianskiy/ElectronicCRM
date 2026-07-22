using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.PreviewProductTypeMigration;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;

namespace ElectronicService.Core.Catalog.Products
    .ApplyProductTypeMigration;

public sealed class
    ApplyProductTypeMigrationCommandHandler
{
    private readonly ProductTypeMigrationPlanner
        _planner;

    private readonly IProductRepository
        _productRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public ApplyProductTypeMigrationCommandHandler(
        ProductTypeMigrationPlanner planner,
        IProductRepository productRepository,
        ProductAuditRecorder auditRecorder)
    {
        _planner = planner;
        _productRepository = productRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        ApplyProductTypeMigrationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ChangedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.CurrentUserIsRequired());
        }

        if (command.ExpectedProductVersion == 0)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ExpectedProductVersion)));
        }

        var planResult = await _planner
            .BuildAsync(
                command.ProductId,
                command.TargetProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (planResult.IsFailure)
        {
            return UnitResult.Failure(
                planResult.Error);
        }

        var plan = planResult.Value;
        var preview = plan.Preview;

        /*
        * Preview содержал версию строки products
        * на момент первоначального анализа.
        *
        * Planner только что повторно загрузил товар.
        */
        if (preview.ProductVersion
            != command.ExpectedProductVersion)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductConcurrencyConflict(
                    command.ProductId));
        }

        /*
         * Проверяем, что товар всё ещё относится
         * к типу, который Technical видел в preview.
         */
        if (preview.CurrentProductTypeId
            != command.ExpectedCurrentProductTypeId)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeMigrationPreviewIsStale());
        }

        var actualRemovedIds =
            preview.RemovedCharacteristics
                .Select(characteristic =>
                    characteristic.DefinitionId)
                .ToHashSet();

        var expectedRemovedIds =
            command
                .ExpectedRemovedCharacteristicDefinitionIds
                .ToHashSet();

        if (!actualRemovedIds.SetEquals(
                expectedRemovedIds))
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeMigrationPreviewIsStale());
        }

        var actualMissingIds =
            preview.MissingRequiredCharacteristics
                .Select(characteristic =>
                    characteristic.DefinitionId)
                .ToHashSet();

        var expectedMissingIds =
            command
                .ExpectedMissingRequiredCharacteristicDefinitionIds
                .ToHashSet();

        if (!actualMissingIds.SetEquals(
                expectedMissingIds))
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeMigrationPreviewIsStale());
        }

        var duplicateDefinitionId =
            command.RequiredValues
                .GroupBy(value =>
                    value.DefinitionId)
                .Where(group =>
                    group.Count() > 1)
                .Select(group =>
                    (Guid?)group.Key)
                .FirstOrDefault();

        if (duplicateDefinitionId.HasValue)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeMigrationDuplicateValue(
                        duplicateDefinitionId.Value));
        }

        var suppliedValuesById =
            command.RequiredValues
                .ToDictionary(
                    value => value.DefinitionId);

        var missingSuppliedDefinitionId =
            actualMissingIds
                .Where(definitionId =>
                    !suppliedValuesById.ContainsKey(
                        definitionId))
                .Select(definitionId =>
                    (Guid?)definitionId)
                .FirstOrDefault();

        if (missingSuppliedDefinitionId.HasValue)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeMigrationRequiredValueMissing(
                        missingSuppliedDefinitionId.Value));
        }

        var unexpectedDefinitionId =
            suppliedValuesById.Keys
                .Where(definitionId =>
                    !actualMissingIds.Contains(
                        definitionId))
                .Select(definitionId =>
                    (Guid?)definitionId)
                .FirstOrDefault();

        if (unexpectedDefinitionId.HasValue)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeMigrationUnexpectedValue(
                        unexpectedDefinitionId.Value));
        }

        var parsedValues =
            new Dictionary<
                Guid,
                CharacteristicValue>();

        foreach (var suppliedValue
                 in suppliedValuesById.Values)
        {
            if (!plan.DefinitionsById.TryGetValue(
                    suppliedValue.DefinitionId,
                    out var definition))
            {
                return UnitResult.Failure(
                    CatalogErrors
                        .CharacteristicDefinitionNotFound(
                            suppliedValue.DefinitionId));
            }

            var parseResult =
                ProductTypeMigrationValueParser.Parse(
                    definition,
                    suppliedValue.Value);

            if (parseResult.IsFailure)
            {
                return UnitResult.Failure(
                    parseResult.Error);
            }

            parsedValues[
                suppliedValue.DefinitionId] =
                    parseResult.Value;
        }

        var beforeSnapshotResult =
            await _auditRecorder
                .CaptureAsync(
                    plan.Product,
                    cancellationToken)
                .ConfigureAwait(false);

        if (beforeSnapshotResult.IsFailure)
        {
            return UnitResult.Failure(
                beforeSnapshotResult.Error);
        }

        var beforeSnapshot =
            beforeSnapshotResult.Value;

        var migrationResult =
            plan.Product.MigrateToProductType(
                plan.TargetProductType,
                plan.DefinitionsById,
                parsedValues);

        if (migrationResult.IsFailure)
        {
            return UnitResult.Failure(
                migrationResult.Error);
        }

        var auditResult =
            await _auditRecorder
                .RecordManualChangeAsync(
                    plan.Product,
                    command.ChangedByUserId,
                    ProductAuditOperation.ProductTypeMigrated,
                    beforeSnapshot,
                    cancellationToken)
                .ConfigureAwait(false);

        if (auditResult.IsFailure)
        {
            return UnitResult.Failure(
                auditResult.Error);
        }

        if (auditResult.Value
            == ProductAuditRecordOutcome.NoChanges)
        {
            /*
             * Доменный объект мог измениться в памяти,
             * например UpdatedAtUtc, но SaveChanges
             * не вызывается. База остаётся неизменной.
             */
            return UnitResult.Success<DomainError>();
        }
        /*
        * Даже после проверки версии другой запрос
        * теоретически может изменить Product до момента
        * выполнения UPDATE.
        *
        * IsRowVersion заставит EF включить xmin
        * в условие UPDATE.
        */
        var saved = await _productRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductConcurrencyConflict(
                    command.ProductId));
        }

        return UnitResult.Success<DomainError>();
    }
}