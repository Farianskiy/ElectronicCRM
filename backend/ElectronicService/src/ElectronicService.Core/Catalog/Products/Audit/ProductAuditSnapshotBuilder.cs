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

    public async Task<Result<
        IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
        DomainError>> BuildManyAsync(
            IReadOnlyCollection<Product> products,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(products);

        if (products.Count == 0)
        {
            return Result.Success<
                IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
                DomainError>(
                    new Dictionary<Guid, ProductAuditSnapshot>());
        }

        var productTypes = await _metadataRepository
            .GetProductTypesByIdsAsync(
                products.Select(product => product.ProductTypeId).Distinct().ToArray(),
                cancellationToken)
            .ConfigureAwait(false);
        var productTypesById = productTypes.ToDictionary(value => value.Id);

        var manufacturers = await _metadataRepository
            .GetManufacturersAsync(cancellationToken)
            .ConfigureAwait(false);
        var manufacturersById = manufacturers.ToDictionary(value => value.Id);

        var definitionIds = products
            .SelectMany(product => product.Characteristics)
            .Select(characteristic => characteristic.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var definitions = await _metadataRepository
            .GetCharacteristicDefinitionsByIdsAsync(
                definitionIds,
                cancellationToken)
            .ConfigureAwait(false);
        var definitionsById = definitions.ToDictionary(value => value.Id);

        var snapshots = new Dictionary<Guid, ProductAuditSnapshot>(products.Count);

        foreach (var product in products)
        {
            if (!productTypesById.TryGetValue(product.ProductTypeId, out var productType))
            {
                return Result.Failure<
                    IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
                    DomainError>(
                        CatalogErrors.ProductTypeNotFound(product.ProductTypeId.ToString()));
            }

            if (!manufacturersById.TryGetValue(product.ManufacturerId, out var manufacturer))
            {
                return Result.Failure<
                    IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
                    DomainError>(
                        CatalogErrors.ManufacturerNotFound(product.ManufacturerId));
            }

            var missingDefinitionId = product.Characteristics
                .Select(characteristic => characteristic.CharacteristicDefinitionId)
                .FirstOrDefault(definitionId => !definitionsById.ContainsKey(definitionId));

            if (missingDefinitionId != Guid.Empty)
            {
                return Result.Failure<
                    IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
                    DomainError>(
                        CatalogErrors.CharacteristicDefinitionNotFound(missingDefinitionId));
            }

            snapshots[product.Id] = ProductAuditSnapshotFactory.Create(
                product,
                productType,
                manufacturer,
                definitionsById);
        }

        return Result.Success<
            IReadOnlyDictionary<Guid, ProductAuditSnapshot>,
            DomainError>(snapshots);
    }
}
