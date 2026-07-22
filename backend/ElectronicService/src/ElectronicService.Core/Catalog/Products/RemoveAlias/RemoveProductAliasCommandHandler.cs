using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.RemoveAlias;

public sealed class RemoveProductAliasCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public RemoveProductAliasCommandHandler(
        IProductRepository productRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RemoveProductAliasCommand command,
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

        if (command.AliasId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.AliasId)));
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

        var beforeJson =
            ProductAuditSnapshotSerializer.Serialize(
                product);

        var removeResult =
            product.RemoveAlias(
                command.AliasId);

        if (removeResult.IsFailure)
        {
            return UnitResult.Failure(
                removeResult.Error);
        }

        var auditResult =
            _auditRecorder.RecordManualChange(
                product,
                command.ChangedByUserId,
                ProductAuditOperation.AliasRemoved,
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