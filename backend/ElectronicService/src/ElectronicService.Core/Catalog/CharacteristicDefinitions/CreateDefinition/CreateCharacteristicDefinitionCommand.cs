namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.CreateDefinition;

public sealed record CreateCharacteristicDefinitionCommand(
    string Code,
    string Name,
    string DataType,
    string? Unit);