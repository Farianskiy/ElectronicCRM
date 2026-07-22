using ElectronicService.Domain.Catalog.Products;

namespace ElectronicService.Core.Catalog.Products.Audit;

public static class ProductAuditSnapshotFactory
{
    public static ProductAuditSnapshot Create(
        Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var characteristics = product.Characteristics
            .OrderBy(characteristic =>
                characteristic
                    .CharacteristicDefinitionId)
            .Select(characteristic =>
                new ProductAuditCharacteristicSnapshot(
                    characteristic
                        .CharacteristicDefinitionId,

                    characteristic.Value
                        .DataType
                        .ToString(),

                    characteristic.Value.TextValue,
                    characteristic.Value.NumberValue,
                    characteristic.Value.BooleanValue))
            .ToList();

        var aliases = product.Aliases
            .OrderBy(alias => alias.Id)
            .Select(alias =>
                new ProductAuditAliasSnapshot(
                    alias.Id,
                    alias.Value))
            .ToList();

        return new ProductAuditSnapshot(
            product.Id,
            product.Article.Value,
            product.Name.Value,
            product.ProductTypeId,
            product.ManufacturerId,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity.Value,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            characteristics,
            aliases);
    }
}