using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.ArchiveProduct;

public sealed class ArchiveProductCommandHandler
{
    private readonly IProductRepository _productRepository;

    public ArchiveProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        ArchiveProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty
            || command.ArchivedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.CurrentUserIsRequired());
        }

        var product = await _productRepository
            .GetByIdIncludingArchivedAsync(command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(command.ProductId.ToString()));
        }

        var archiveResult = product.Archive(
            command.ArchivedByUserId,
            command.Reason);

        if (archiveResult.IsFailure)
        {
            return archiveResult;
        }

        var saved = await _productRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return saved
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure(
                CatalogErrors.ProductConcurrencyConflict(command.ProductId));
    }
}
