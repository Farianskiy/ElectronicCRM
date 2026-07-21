namespace ElectronicService.Core.Catalog.Products
    .RemoveCharacteristic;

public sealed record RemoveProductCharacteristicCommand(
    Guid ProductId,
    string Code);