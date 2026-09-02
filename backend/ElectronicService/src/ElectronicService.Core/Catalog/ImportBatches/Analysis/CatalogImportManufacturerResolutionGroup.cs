using ElectronicService.Core.Catalog.Manufacturers.Resolution;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportManufacturerResolutionGroup(
    string SourceValue,
    string NormalizedSourceValue,
    ManufacturerResolutionStatus Status,
    Guid? ManufacturerId,
    string? ResolvedManufacturerName,
    ManufacturerResolutionSource Source,
    Guid? ManufacturerAliasId,
    Guid? ManufacturerNoisePhraseId,
    string? NoiseReason,
    int OccurrenceCount,
    IReadOnlyCollection<int> ExampleRowNumbers,
    IReadOnlyCollection<string> ExampleProductNames);