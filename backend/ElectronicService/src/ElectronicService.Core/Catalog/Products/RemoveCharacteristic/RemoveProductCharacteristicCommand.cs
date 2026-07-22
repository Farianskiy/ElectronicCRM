namespace ElectronicService.Core.Catalog.Products
    .RemoveCharacteristic;

public sealed record RemoveProductCharacteristicCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    string Code);