namespace ElectronicService.Contracts.Catalog.Manufacturers;

public sealed class CreateManufacturerFromUnresolvedPhraseRequest
{
    public string CanonicalName { get; init; } = string.Empty;

    public string SourcePhrase { get; init; } = string.Empty;
}