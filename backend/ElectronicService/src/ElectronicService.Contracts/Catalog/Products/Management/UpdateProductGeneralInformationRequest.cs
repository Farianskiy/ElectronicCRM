namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record UpdateProductGeneralInformationRequest(
    string Name,
    string Article,
    Guid ManufacturerId);