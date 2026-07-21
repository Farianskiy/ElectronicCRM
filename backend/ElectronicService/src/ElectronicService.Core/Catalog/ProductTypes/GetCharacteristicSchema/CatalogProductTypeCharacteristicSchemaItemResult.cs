namespace ElectronicService.Core.Catalog.ProductTypes
    .GetCharacteristicSchema;

public sealed record CatalogProductTypeCharacteristicSchemaItemResult(
    Guid DefinitionId,
    string Code,
    string Name,
    string DataType,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsUsedForReplacement,
    string ReplacementMatchMode,
    int ReplacementWeight,
    int ProductsWithValueCount,
    int ProductsWithoutValueCount);