namespace ElectronicService.Contracts.Catalog
    .CharacteristicDefinitions;

public sealed record
    UpdateCharacteristicDefinitionRequest(
        string Name,
        string? Unit);