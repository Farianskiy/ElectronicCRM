namespace ElectronicService.Core.Catalog.Products.SetCharacteristic;

public sealed record SetProductCharacteristicCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    string Code,
    string Value);