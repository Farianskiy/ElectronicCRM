namespace ElectronicService.Core.Catalog.Manufacturers.CreateApprovedAlias;

public sealed record CreateApprovedManufacturerAliasCommand(
    Guid ManufacturerId,
    string Phrase);