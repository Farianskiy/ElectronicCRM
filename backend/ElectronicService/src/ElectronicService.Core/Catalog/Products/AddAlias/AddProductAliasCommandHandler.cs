using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.AddAlias;

public sealed class AddProductAliasCommandHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly ProductAuditRecorder
        _auditRecorder;

    public AddProductAliasCommandHandler(
        IProductRepository productRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<UnitResult<DomainError>> Handle(
        AddProductAliasCommand command,
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

        if (string.IsNullOrWhiteSpace(command.Alias))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.Alias)));
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

        var addAliasResult =
            product.AddAlias(
                command.Alias);

        if (addAliasResult.IsFailure)
        {
            return UnitResult.Failure(
                addAliasResult.Error);
        }

        var auditResult =
            _auditRecorder.RecordManualChange(
                product,
                command.ChangedByUserId,
                ProductAuditOperation.AliasAdded,
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