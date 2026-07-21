namespace ElectronicService.Core.Catalog.ProductTypes
    .AddOptionalCharacteristic;

public sealed record
    AddOptionalCharacteristicToProductTypeCommand(
        string ProductTypeCode,
        Guid CharacteristicDefinitionId);