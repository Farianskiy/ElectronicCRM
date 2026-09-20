using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.CreateRuleSetVersion;

public sealed record CreateCatalogRecognitionRuleSetVersionCommand(
    Guid ManufacturerId,
    Guid ProductTypeId,
    string Name,
    IReadOnlyList<CatalogRecognitionRuleSetEntryData> Entries);