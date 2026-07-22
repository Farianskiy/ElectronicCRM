using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.Products.Audit;

public static class ProductAuditSnapshotFactory
{
    public static ProductAuditSnapshot Create(
        Product product,
        ProductType productType,
        Manufacturer manufacturer,
        IReadOnlyDictionary<
            Guid,
            CharacteristicDefinition>
            definitionsById)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(manufacturer);
        ArgumentNullException.ThrowIfNull(definitionsById);

        var characteristics = product.Characteristics
            .OrderBy(characteristic =>
                characteristic
                    .CharacteristicDefinitionId)
            .Select(characteristic =>
            {
                var definition =
                    definitionsById[
                        characteristic
                            .CharacteristicDefinitionId];

                return new
                    ProductAuditCharacteristicSnapshot(
                        definition.Id,
                        definition.Code,
                        definition.Name,
                        characteristic.Value
                            .DataType
                            .ToString(),
                        definition.Unit,
                        characteristic.Value.TextValue,
                        characteristic.Value.NumberValue,
                        characteristic.Value.BooleanValue);
            })
            .ToList();

        var aliases = product.Aliases
            .OrderBy(alias => alias.Id)
            .Select(alias =>
                new ProductAuditAliasSnapshot(
                    alias.Id,
                    alias.Value))
            .ToList();

        return new ProductAuditSnapshot(
            ProductAuditSnapshotVersions.Current,

            product.Id,
            product.Article.Value,
            product.Name.Value,

            product.ProductTypeId,
            productType.Code,
            productType.Name,

            product.ManufacturerId,
            manufacturer.Name,

            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity.Value,

            characteristics,
            aliases);
    }
}