namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.UpdateDefinition;

public sealed record UpdateCharacteristicDefinitionCommand(
    Guid CharacteristicDefinitionId,
    string Name,
    string? Unit);