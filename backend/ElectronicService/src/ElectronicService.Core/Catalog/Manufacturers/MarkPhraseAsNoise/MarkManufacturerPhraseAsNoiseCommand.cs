namespace ElectronicService.Core.Catalog.Manufacturers.MarkPhraseAsNoise;

public sealed record MarkManufacturerPhraseAsNoiseCommand(
    string Phrase,
    string? Reason);