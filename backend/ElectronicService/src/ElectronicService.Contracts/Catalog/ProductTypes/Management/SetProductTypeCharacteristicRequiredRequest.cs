namespace ElectronicService.Contracts.Catalog
    .ProductTypes.Management;

public sealed record
    SetProductTypeCharacteristicRequiredRequest(
        bool IsRequired);