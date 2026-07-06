namespace ElectronicService.Contracts.Catalog.Products;

public sealed record ProductCharacteristicResponse(
    string Code,
    string Name,
    string DataType,
    string? Unit,
    string Value);