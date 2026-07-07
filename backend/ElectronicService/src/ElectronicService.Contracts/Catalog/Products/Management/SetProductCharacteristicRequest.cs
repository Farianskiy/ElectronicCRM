namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record SetProductCharacteristicRequest(
    string Code,
    string Value);