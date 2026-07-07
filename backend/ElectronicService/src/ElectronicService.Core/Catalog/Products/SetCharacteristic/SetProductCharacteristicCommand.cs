namespace ElectronicService.Core.Catalog.Products.SetCharacteristic;

public sealed record SetProductCharacteristicCommand(
    Guid ProductId,
    string Code,
    string Value);