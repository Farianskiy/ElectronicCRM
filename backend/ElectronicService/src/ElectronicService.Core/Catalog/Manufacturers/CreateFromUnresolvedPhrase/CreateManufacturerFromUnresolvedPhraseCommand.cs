namespace ElectronicService.Core.Catalog.Manufacturers.CreateFromUnresolvedPhrase;

public sealed record CreateManufacturerFromUnresolvedPhraseCommand(
    string CanonicalName,
    string SourcePhrase);