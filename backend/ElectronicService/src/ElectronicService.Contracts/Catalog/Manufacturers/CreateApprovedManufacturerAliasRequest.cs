namespace ElectronicService.Contracts.Catalog.Manufacturers;

public sealed class CreateApprovedManufacturerAliasRequest
{
    public Guid ManufacturerId { get; init; }

    public string Phrase { get; init; } = string.Empty;
}