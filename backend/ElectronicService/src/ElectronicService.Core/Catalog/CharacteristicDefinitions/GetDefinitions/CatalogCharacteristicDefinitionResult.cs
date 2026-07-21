namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.GetDefinitions;

public sealed record CatalogCharacteristicDefinitionResult(
    Guid Id,
    string Code,
    string Name,
    string DataType,
    string? Unit,
    int ProductTypesCount,
    int ProductsWithValueCount);