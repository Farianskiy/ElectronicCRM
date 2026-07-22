namespace ElectronicService.Core.Catalog.Products
    .UpdateGeneralInformation;

public sealed record UpdateProductGeneralInformationCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    string Name,
    string Article,
    Guid ManufacturerId);