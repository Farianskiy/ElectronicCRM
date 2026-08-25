namespace ElectronicService.Core.Catalog.Manufacturers.CreateApprovedAlias;

public sealed record CreateApprovedManufacturerAliasResult(
    Guid ManufacturerAliasId,
    Guid ManufacturerId,
    string ManufacturerName,
    string Phrase,
    string NormalizedPhrase,
    string Status,
    string Source);