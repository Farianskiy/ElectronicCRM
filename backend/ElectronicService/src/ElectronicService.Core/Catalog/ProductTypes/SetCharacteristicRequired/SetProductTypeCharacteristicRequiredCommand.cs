namespace ElectronicService.Core.Catalog.ProductTypes
    .SetCharacteristicRequired;

public sealed record
    SetProductTypeCharacteristicRequiredCommand(
        string ProductTypeCode,
        Guid CharacteristicDefinitionId,
        bool IsRequired);