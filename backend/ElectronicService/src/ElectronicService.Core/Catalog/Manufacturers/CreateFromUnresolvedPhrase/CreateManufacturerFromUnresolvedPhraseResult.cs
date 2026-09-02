namespace ElectronicService.Core.Catalog.Manufacturers.CreateFromUnresolvedPhrase;

public sealed record CreateManufacturerFromUnresolvedPhraseResult(
    Guid ManufacturerId,
    string ManufacturerName,
    string NormalizedManufacturerName,
    Guid? ManufacturerAliasId,
    string? AliasPhrase,
    string? NormalizedAliasPhrase,
    string? AliasStatus,
    string? AliasSource,
    string ResolutionSource);