using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.CreateProductType;

public sealed class CreateProductTypeCommandHandler
{
    private readonly IProductTypeManagementRepository _repository;

    public CreateProductTypeCommandHandler(
        IProductTypeManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreateProductTypeResult, DomainError>> Handle(
        CreateProductTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var createResult = ProductType.Create(
            command.Code,
            command.Name);

        if (createResult.IsFailure)
        {
            return Result.Failure<CreateProductTypeResult, DomainError>(
                createResult.Error);
        }

        var productType = createResult.Value;

        var alreadyExists = await _repository
            .ExistsByCodeAsync(productType.Code, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return Result.Failure<CreateProductTypeResult, DomainError>(
                CatalogErrors.ProductTypeAlreadyExists(productType.Code));
        }

        _repository.Add(productType);

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<CreateProductTypeResult, DomainError>(
            new CreateProductTypeResult(
                productType.Id,
                productType.Code,
                productType.Name));
    }
}
