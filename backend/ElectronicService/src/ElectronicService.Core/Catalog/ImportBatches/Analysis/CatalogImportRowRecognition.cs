using ElectronicService.Core.Catalog.Recognition.Effective;
using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRowRecognition(
    CatalogImportNormalizedRowData Input,
    IReadOnlyCollection<CharacteristicDefinition> AllowedCharacteristics,
    CatalogEffectiveRecognitionResult Result);
