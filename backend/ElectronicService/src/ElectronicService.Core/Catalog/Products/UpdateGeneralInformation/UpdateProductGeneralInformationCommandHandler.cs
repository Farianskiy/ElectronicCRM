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
        var beforeJson =
            ProductAuditSnapshotSerializer.Serialize(
                product);

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
            _auditRecorder.RecordManualChange(
                product,
                command.ChangedByUserId,
                ProductAuditOperation
                    .GeneralInformationUpdated,
                beforeJson);

        if (auditResult.IsFailure)
        {
            return UnitResult.Failure(
                auditResult.Error);
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