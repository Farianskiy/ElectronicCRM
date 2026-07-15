namespace ElectronicService.Infrastructure.Postgres.Catalog.Import;

internal sealed record ManufacturerNormalizationResult(
    string RawName,
    string NormalizedName,
    bool WasChanged);