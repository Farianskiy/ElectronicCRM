namespace ElectronicService.Contracts.Catalog.ProductTypes;

public sealed record
    CatalogProductTypeCharacteristicSchemaItemResponse(
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
        int ProductsWithoutValueCount,
        bool CanMakeRequired,
        bool CanRemoveFromType);