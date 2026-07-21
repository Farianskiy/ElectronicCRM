namespace ElectronicService.Core.Catalog.ProductTypes
    .GetAvailableCharacteristicDefinitions;

public sealed record AvailableCharacteristicDefinitionResult(
    Guid Id,
    string Code,
    string Name,
    string DataType,
    string? Unit);