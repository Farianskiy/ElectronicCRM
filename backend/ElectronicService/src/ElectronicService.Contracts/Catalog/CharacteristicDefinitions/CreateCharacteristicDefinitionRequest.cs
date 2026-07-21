namespace ElectronicService.Contracts.Catalog.CharacteristicDefinitions;

public sealed record
    CreateCharacteristicDefinitionRequest(
        string Code,
        string Name,
        string DataType,
        string? Unit);