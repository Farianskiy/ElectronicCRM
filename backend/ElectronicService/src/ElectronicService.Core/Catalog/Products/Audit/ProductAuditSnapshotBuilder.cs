using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products
    .Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Audit;

public sealed class ProductAuditSnapshotBuilder
{
    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    public ProductAuditSnapshotBuilder(
        ICatalogProductMetadataRepository
            metadataRepository)
    {
        _metadataRepository = metadataRepository;
    }

    public async Task<Result<
        ProductAuditSnapshot,
        DomainError>> BuildAsync(
            Product product,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(product);

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(
                product.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<
                ProductAuditSnapshot,
                DomainError>(
                    CatalogErrors.ProductTypeNotFound(
                        product
                            .ProductTypeId
                            .ToString()));
        }

        var manufacturer =
            await _metadataRepository
                .GetManufacturerByIdAsync(
                    product.ManufacturerId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (manufacturer is null)
        {
            return Result.Failure<
                ProductAuditSnapshot,
                DomainError>(
                    CatalogErrors.ManufacturerNotFound(
                        product.ManufacturerId));
        }

        var definitionIds = product.Characteristics
            .Select(characteristic =>
                characteristic
                    .CharacteristicDefinitionId)
            .Distinct()
            .ToList();

        IReadOnlyCollection<
            CharacteristicDefinition> definitions =
                definitionIds.Count == 0
                    ? []
                    : await _metadataRepository
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
                ProductAuditSnapshot,
                DomainError>(
                    CatalogErrors
                        .CharacteristicDefinitionNotFound(
                            missingDefinitionId.Value));
        }

        var snapshot =
            ProductAuditSnapshotFactory.Create(
                product,
                productType,
                manufacturer,
                definitionsById);

        return Result.Success<
            ProductAuditSnapshot,
            DomainError>(snapshot);
    }
}