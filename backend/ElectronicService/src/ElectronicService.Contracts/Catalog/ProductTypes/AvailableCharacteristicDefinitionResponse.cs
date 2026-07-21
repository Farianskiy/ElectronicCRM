namespace ElectronicService.Contracts.Catalog.ProductTypes;

public sealed record
    AvailableCharacteristicDefinitionResponse(
        Guid Id,
        string Code,
        string Name,
        string DataType,
        string? Unit);