namespace ElectronicService.Contracts.Catalog.Manufacturers;

public sealed record CreateManufacturerFromUnresolvedPhraseResponse(
    Guid ManufacturerId,
    string ManufacturerName,
    string NormalizedManufacturerName,
    Guid? ManufacturerAliasId,
    string? AliasPhrase,
    string? NormalizedAliasPhrase,
    string? AliasStatus,
    string? AliasSource,
    string ResolutionSource);