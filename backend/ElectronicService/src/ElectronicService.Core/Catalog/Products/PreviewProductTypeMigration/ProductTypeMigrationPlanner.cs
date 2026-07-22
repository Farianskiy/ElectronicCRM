using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed class ProductTypeMigrationPlanner
{
    private readonly IProductRepository
        _productRepository;

    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    public ProductTypeMigrationPlanner(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository
            metadataRepository)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
    }

    public async Task<Result<
        ProductTypeMigrationPlan,
        DomainError>> BuildAsync(
            Guid productId,
            Guid targetProductTypeId,
            CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(productId)));
        }

        if (targetProductTypeId == Guid.Empty)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(targetProductTypeId)));
        }

        var product = await _productRepository
            .GetByIdWithDetailsAsync(
                productId,
                cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    CatalogErrors.ProductNotFound(
                        productId.ToString()));
        }

        var currentProductType =
            await _metadataRepository
                .GetProductTypeByIdAsync(
                    product.ProductTypeId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentProductType is null)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    CatalogErrors.ProductTypeNotFound(
                        product.ProductTypeId
                            .ToString()));
        }

        var targetProductType =
            await _metadataRepository
                .GetProductTypeByIdAsync(
                    targetProductTypeId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (targetProductType is null)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    CatalogErrors.ProductTypeNotFound(
                        targetProductTypeId
                            .ToString()));
        }

        if (currentProductType.Id
            == targetProductType.Id)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    CatalogErrors
                        .ProductTypeMigrationTargetMustBeDifferent(
                            targetProductType.Id));
        }

        var definitionIds = product.Characteristics
            .Select(characteristic =>
                characteristic
                    .CharacteristicDefinitionId)
            .Concat(
                targetProductType.Characteristics
                    .Select(characteristic =>
                        characteristic
                            .CharacteristicDefinitionId))
            .Distinct()
            .ToArray();

        var definitions = await _metadataRepository
            .GetCharacteristicDefinitionsByIdsAsync(
                definitionIds,
                cancellationToken)
            .ConfigureAwait(false);

        var definitionsById = definitions
            .ToDictionary(
                definition => definition.Id);

        var missingDefinitionId = definitionIds
            .Where(definitionId =>
                !definitionsById.ContainsKey(
                    definitionId))
            .Select(definitionId =>
                (Guid?)definitionId)
            .FirstOrDefault();

        if (missingDefinitionId.HasValue)
        {
            return Result.Failure<
                ProductTypeMigrationPlan,
                DomainError>(
                    CatalogErrors
                        .CharacteristicDefinitionNotFound(
                            missingDefinitionId.Value));
        }

        var targetDefinitionIds =
            targetProductType.Characteristics
                .Select(characteristic =>
                    characteristic
                        .CharacteristicDefinitionId)
                .ToHashSet();

        var currentDefinitionIds =
            product.Characteristics
                .Select(characteristic =>
                    characteristic
                        .CharacteristicDefinitionId)
                .ToHashSet();

        var currentValueItems =
            product.Characteristics
                .Select(productCharacteristic =>
                {
                    var definition =
                        definitionsById[
                            productCharacteristic
                                .CharacteristicDefinitionId];

                    return new
                    {
                        DefinitionId = definition.Id,

                        Result =
                            new ProductTypeMigrationCharacteristicValueResult(
                                definition.Id,
                                definition.Code,
                                definition.Name,
                                definition
                                    .DataType
                                    .ToString(),
                                definition.Unit,
                                productCharacteristic
                                    .Value
                                    .ToString())
                    };
                })
                .ToList();

        var preservedCharacteristics =
            currentValueItems
                .Where(item =>
                    targetDefinitionIds.Contains(
                        item.DefinitionId))
                .Select(item => item.Result)
                .OrderBy(
                    item => item.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        var removedCharacteristics =
            currentValueItems
                .Where(item =>
                    !targetDefinitionIds.Contains(
                        item.DefinitionId))
                .Select(item => item.Result)
                .OrderBy(
                    item => item.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        var missingRequiredCharacteristics =
            targetProductType.Characteristics
                .Where(characteristic =>
                    characteristic.IsRequired)
                .Select(characteristic =>
                    characteristic
                        .CharacteristicDefinitionId)
                .Where(definitionId =>
                    !currentDefinitionIds.Contains(
                        definitionId))
                .Select(definitionId =>
                {
                    var definition =
                        definitionsById[definitionId];

                    return new
                        ProductTypeMigrationMissingRequiredCharacteristicResult(
                            definition.Id,
                            definition.Code,
                            definition.Name,
                            definition
                                .DataType
                                .ToString(),
                            definition.Unit);
                })
                .OrderBy(
                    item => item.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        var preview =
            new ProductTypeMigrationPreviewResult(
                product.Id,
                product.Version,

                currentProductType.Id,
                currentProductType.Code,
                currentProductType.Name,

                targetProductType.Id,
                targetProductType.Code,
                targetProductType.Name,

                missingRequiredCharacteristics.Count == 0,

                preservedCharacteristics,
                removedCharacteristics,
                missingRequiredCharacteristics);

        var plan = new ProductTypeMigrationPlan(
            product,
            currentProductType,
            targetProductType,
            definitionsById,
            preview);

        return Result.Success<
            ProductTypeMigrationPlan,
            DomainError>(plan);
    }
}