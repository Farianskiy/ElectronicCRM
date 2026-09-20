using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetVersionSaveData(
    Guid ManufacturerId,
    Guid ProductTypeId,
    string Name,
    Guid CreatedByUserId,
    IReadOnlyList<CatalogRecognitionRuleSetEntryData> Entries);

public sealed record CatalogRecognitionRuleSetVersionSaveResult(
    Guid Id,
    int VersionNumber);