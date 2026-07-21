namespace ElectronicService.Core.Catalog.ProductTypes
    .RemoveCharacteristic;

public sealed record
    RemoveCharacteristicFromProductTypeCommand(
        string ProductTypeCode,
        Guid CharacteristicDefinitionId);