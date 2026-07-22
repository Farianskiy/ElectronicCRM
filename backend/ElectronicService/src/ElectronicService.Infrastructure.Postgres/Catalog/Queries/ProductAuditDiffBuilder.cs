using System.Globalization;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Catalog.Products
    .GetAuditHistory;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Queries;

internal static class ProductAuditDiffBuilder
{
    public static IReadOnlyCollection<
        ProductAuditHistoryChangeResult> Build(
            ProductAuditSnapshot? before,
            ProductAuditSnapshot? after,
            IReadOnlyDictionary<Guid, string>
                productTypeNames,
            IReadOnlyDictionary<Guid, string>
                manufacturerNames,
            IReadOnlyDictionary<
                Guid,
                ProductAuditCharacteristicMetadata>
                characteristicDefinitions)
    {
        var changes =
            new List<
                ProductAuditHistoryChangeResult>();

        AddChange(
            changes,
            "name",
            "Название",
            before?.Name,
            after?.Name);

        AddChange(
            changes,
            "article",
            "Артикул",
            before?.Article,
            after?.Article);

        AddChange(
            changes,
            "manufacturer",
            "Производитель",
            FormatReference(
                before?.ManufacturerId,
                manufacturerNames),
            FormatReference(
                after?.ManufacturerId,
                manufacturerNames));

        AddChange(
            changes,
            "productType",
            "Тип товара",
            FormatReference(
                before?.ProductTypeId,
                productTypeNames),
            FormatReference(
                after?.ProductTypeId,
                productTypeNames));

        AddChange(
            changes,
            "price",
            "Цена",
            FormatPrice(before),
            FormatPrice(after));

        AddChange(
            changes,
            "stock",
            "Остаток",
            FormatStock(before),
            FormatStock(after));

        AddCharacteristicChanges(
            changes,
            before,
            after,
            characteristicDefinitions);

        AddAliasChanges(
            changes,
            before,
            after);

        return changes;
    }

    private static void AddCharacteristicChanges(
        ICollection<
            ProductAuditHistoryChangeResult> changes,
        ProductAuditSnapshot? before,
        ProductAuditSnapshot? after,
        IReadOnlyDictionary<
            Guid,
            ProductAuditCharacteristicMetadata>
            definitions)
    {
        var beforeCharacteristics =
            (before?.Characteristics
                ?? [])
            .GroupBy(characteristic =>
                characteristic.DefinitionId)
            .ToDictionary(
                group => group.Key,
                group => group.Last());

        var afterCharacteristics =
            (after?.Characteristics
                ?? [])
            .GroupBy(characteristic =>
                characteristic.DefinitionId)
            .ToDictionary(
                group => group.Key,
                group => group.Last());

        var definitionIds = beforeCharacteristics
            .Keys
            .Concat(afterCharacteristics.Keys)
            .Distinct()
            .OrderBy(
                definitionId =>
                    GetCharacteristicLabel(
                        definitionId,
                        definitions),
                StringComparer.Ordinal)
            .ToList();

        foreach (var definitionId
                 in definitionIds)
        {
            beforeCharacteristics.TryGetValue(
                definitionId,
                out var beforeValue);

            afterCharacteristics.TryGetValue(
                definitionId,
                out var afterValue);

            definitions.TryGetValue(
                definitionId,
                out var metadata);

            AddChange(
                changes,
                $"characteristic:{definitionId}",
                GetCharacteristicLabel(
                    definitionId,
                    definitions),
                FormatCharacteristicValue(
                    beforeValue,
                    metadata),
                FormatCharacteristicValue(
                    afterValue,
                    metadata));
        }
    }

    private static void AddAliasChanges(
        ICollection<
            ProductAuditHistoryChangeResult> changes,
        ProductAuditSnapshot? before,
        ProductAuditSnapshot? after)
    {
        var beforeAliases =
            (before?.Aliases ?? [])
            .GroupBy(alias => alias.AliasId)
            .ToDictionary(
                group => group.Key,
                group => group.Last());

        var afterAliases =
            (after?.Aliases ?? [])
            .GroupBy(alias => alias.AliasId)
            .ToDictionary(
                group => group.Key,
                group => group.Last());

        var aliasIds = beforeAliases
            .Keys
            .Concat(afterAliases.Keys)
            .Distinct()
            .OrderBy(aliasId => aliasId)
            .ToList();

        foreach (var aliasId in aliasIds)
        {
            beforeAliases.TryGetValue(
                aliasId,
                out var beforeAlias);

            afterAliases.TryGetValue(
                aliasId,
                out var afterAlias);

            AddChange(
                changes,
                $"alias:{aliasId}",
                "Псевдоним",
                beforeAlias?.Value,
                afterAlias?.Value);
        }
    }

    private static string GetCharacteristicLabel(
        Guid definitionId,
        IReadOnlyDictionary<
            Guid,
            ProductAuditCharacteristicMetadata>
            definitions)
    {
        if (!definitions.TryGetValue(
                definitionId,
                out var metadata))
        {
            return $"Характеристика {definitionId}";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{metadata.Name} ({metadata.Code})");
    }

    private static string?
        FormatCharacteristicValue(
            ProductAuditCharacteristicSnapshot?
                characteristic,
            ProductAuditCharacteristicMetadata?
                metadata)
    {
        if (characteristic is null)
        {
            return null;
        }

        string? value = null;

        if (characteristic.TextValue is not null)
        {
            value = characteristic.TextValue;
        }
        else if (characteristic.NumberValue.HasValue)
        {
            value = characteristic
                .NumberValue
                .Value
                .ToString(
                    CultureInfo.InvariantCulture);
        }
        else if (characteristic.BooleanValue.HasValue)
        {
            value = characteristic
                .BooleanValue
                .Value
                    ? "Да"
                    : "Нет";
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(
                metadata?.Unit))
        {
            return value;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{value} {metadata.Unit}");
    }

    private static string? FormatReference(
        Guid? id,
        IReadOnlyDictionary<Guid, string> names)
    {
        if (!id.HasValue)
        {
            return null;
        }

        return names.TryGetValue(
            id.Value,
            out var name)
                ? name
                : id.Value.ToString();
    }

    private static string? FormatPrice(
        ProductAuditSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{snapshot.PriceAmount} " +
            $"{snapshot.PriceCurrency}");
    }

    private static string? FormatStock(
        ProductAuditSnapshot? snapshot)
    {
        return snapshot?.StockQuantity
            .ToString(
                CultureInfo.InvariantCulture);
    }

    private static void AddChange(
        ICollection<
            ProductAuditHistoryChangeResult> changes,
        string field,
        string label,
        string? before,
        string? after)
    {
        if (string.Equals(
                before,
                after,
                StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(
            new ProductAuditHistoryChangeResult(
                field,
                label,
                before,
                after));
    }
}

internal sealed record
    ProductAuditCharacteristicMetadata(
        string Code,
        string Name,
        string? Unit);