using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.AddAlias;

public sealed class AddProductAliasCommandHandler
{
    private readonly IProductRepository _productRepository;

    public AddProductAliasCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        AddProductAliasCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.ProductId)));
        }

        if (string.IsNullOrWhiteSpace(command.Alias))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.Alias)));
        }

        var product = await _productRepository
            .GetByIdWithDetailsAsync(command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(command.ProductId.ToString()));
        }

        var addAliasResult = product.AddAlias(command.Alias);

        if (addAliasResult.IsFailure)
        {
            return UnitResult.Failure(addAliasResult.Error);
        }

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}