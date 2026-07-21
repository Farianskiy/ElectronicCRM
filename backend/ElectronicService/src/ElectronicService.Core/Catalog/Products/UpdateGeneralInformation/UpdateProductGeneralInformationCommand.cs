namespace ElectronicService.Core.Catalog.Products
    .UpdateGeneralInformation;

public sealed record UpdateProductGeneralInformationCommand(
    Guid ProductId,
    string Name,
    string Article,
    Guid ManufacturerId);