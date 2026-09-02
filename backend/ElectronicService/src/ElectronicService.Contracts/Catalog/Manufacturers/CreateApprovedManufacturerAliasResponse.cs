namespace ElectronicService.Contracts.Catalog.Manufacturers;

public sealed record CreateApprovedManufacturerAliasResponse(
    Guid ManufacturerAliasId,
    Guid ManufacturerId,
    string ManufacturerName,
    string Phrase,
    string NormalizedPhrase,
    string Status,
    string Source);