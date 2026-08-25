namespace ElectronicService.Contracts.Catalog.Manufacturers;

public sealed record MarkManufacturerPhraseAsNoiseResponse(
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
    string Action);