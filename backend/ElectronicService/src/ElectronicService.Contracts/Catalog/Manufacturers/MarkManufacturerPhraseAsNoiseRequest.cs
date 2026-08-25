namespace ElectronicService.Contracts.Catalog.Manufacturers;

public sealed record MarkManufacturerPhraseAsNoiseRequest(
    string Phrase,
    string? Reason);