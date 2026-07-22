using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .UpdateGeneralInformation;

public sealed class
    UpdateProductGeneralInformationCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public UpdateProductGeneralInformationCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateProductGeneralInformationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ProductId)));
        }

        if (command.ChangedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.CurrentUserIsRequired());
        }

        if (command.ManufacturerId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.ManufacturerId)));
        }

        var product = await _productRepository
            .GetByIdWithDetailsAsync(
                command.ProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(
                    command.ProductId.ToString()));
        }

        /*
         * Проверяем, что новый производитель
         * существует.
         */
        var manufacturer = await _metadataRepository
            .GetManufacturerByIdAsync(
                command.ManufacturerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (manufacturer is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ManufacturerNotFound(
                    command.ManufacturerId));
        }

        /*
         * Снимок делается после проверки
         * производителя, но до изменения Product.
         */
        var beforeSnapshotResult =
            await _auditRecorder
                .CaptureAsync(
                    product,
                    cancellationToken)
                .ConfigureAwait(false);

        if (beforeSnapshotResult.IsFailure)
        {
            return UnitResult.Failure(
                beforeSnapshotResult.Error);
        }

        var beforeSnapshot =
            beforeSnapshotResult.Value;

        var updateResult =
            product.UpdateGeneralInformation(
                command.Name,
                command.Article,
                manufacturer.Id);

        if (updateResult.IsFailure)
        {
            return UnitResult.Failure(
                updateResult.Error);
        }

        var auditResult =
            await _auditRecorder
                .RecordManualChangeAsync(
                    product,
                    command.ChangedByUserId,
                    ProductAuditOperation.GeneralInformationUpdated,
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