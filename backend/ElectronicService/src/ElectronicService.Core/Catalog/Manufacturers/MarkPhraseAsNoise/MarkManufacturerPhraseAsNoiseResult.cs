namespace ElectronicService.Core.Catalog.Manufacturers.MarkPhraseAsNoise;

public sealed record MarkManufacturerPhraseAsNoiseResult(
    Guid ManufacturerNoisePhraseId,
    string Phrase,
    string NormalizedPhrase,
    string? Reason,
    bool IsActive,
    Guid CreatedByUserId,
    Guid UpdatedByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? DeactivatedAtUtc,
    MarkManufacturerPhraseAsNoiseAction Action);