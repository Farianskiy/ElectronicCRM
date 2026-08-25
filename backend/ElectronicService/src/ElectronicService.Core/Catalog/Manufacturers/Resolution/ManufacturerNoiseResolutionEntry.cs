namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed record ManufacturerNoiseResolutionEntry(
    Guid ManufacturerNoisePhraseId,
    string NormalizedInputName,
    string? Reason);