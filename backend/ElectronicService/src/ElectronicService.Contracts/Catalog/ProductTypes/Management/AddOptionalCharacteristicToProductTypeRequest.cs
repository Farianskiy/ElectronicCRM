namespace ElectronicService.Contracts.Catalog
    .ProductTypes.Management;

public sealed record
    AddOptionalCharacteristicToProductTypeRequest(
        Guid CharacteristicDefinitionId);