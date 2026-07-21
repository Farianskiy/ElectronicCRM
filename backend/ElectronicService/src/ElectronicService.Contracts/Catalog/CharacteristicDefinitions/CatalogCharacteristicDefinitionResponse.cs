namespace ElectronicService.Contracts.Catalog.CharacteristicDefinitions;

public sealed record
    CatalogCharacteristicDefinitionResponse(
        Guid Id,
        string Code,
        string Name,
        string DataType,
        string? Unit,
        int ProductTypesCount,
        int ProductsWithValueCount);