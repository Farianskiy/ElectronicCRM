namespace ElectronicService.Core.Catalog.Products.Audit;

public static class ProductAuditSnapshotComparer
{
    public static bool HasMeaningfulChanges(
        ProductAuditSnapshot before,
        ProductAuditSnapshot after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (before.ProductId != after.ProductId)
        {
            return true;
        }

        if (!string.Equals(
                before.Article,
                after.Article,
                StringComparison.Ordinal)
            || !string.Equals(
                before.Name,
                after.Name,
                StringComparison.Ordinal)
            || before.ProductTypeId
                != after.ProductTypeId
            || before.ManufacturerId
                != after.ManufacturerId
            || before.PriceAmount
                != after.PriceAmount
            || !string.Equals(
                before.PriceCurrency,
                after.PriceCurrency,
                StringComparison.Ordinal)
            || before.StockQuantity
                != after.StockQuantity)
        {
            return true;
        }

        var beforeCharacteristics =
            before.Characteristics
                .OrderBy(characteristic =>
                    characteristic.DefinitionId)
                .Select(characteristic =>
                    (
                        characteristic.DefinitionId,
                        characteristic.DataType,
                        characteristic.TextValue,
                        characteristic.NumberValue,
                        characteristic.BooleanValue
                    ));

        var afterCharacteristics =
            after.Characteristics
                .OrderBy(characteristic =>
                    characteristic.DefinitionId)
                .Select(characteristic =>
                    (
                        characteristic.DefinitionId,
                        characteristic.DataType,
                        characteristic.TextValue,
                        characteristic.NumberValue,
                        characteristic.BooleanValue
                    ));

        if (!beforeCharacteristics.SequenceEqual(
                afterCharacteristics))
        {
            return true;
        }

        var beforeAliases = before.Aliases
            .OrderBy(alias => alias.AliasId)
            .Select(alias =>
                (
                    alias.AliasId,
                    alias.Value
                ));

        var afterAliases = after.Aliases
            .OrderBy(alias => alias.AliasId)
            .Select(alias =>
                (
                    alias.AliasId,
                    alias.Value
                ));

        return !beforeAliases.SequenceEqual(
            afterAliases);
    }
}