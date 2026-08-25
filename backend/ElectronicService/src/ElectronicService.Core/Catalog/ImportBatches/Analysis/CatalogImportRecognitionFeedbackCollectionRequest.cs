using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRecognitionFeedbackCollectionRequest(
    Guid ImportBatchId,
    Guid ImportRowId,
    ProductType ProductType,
    IReadOnlyCollection<CharacteristicDefinition> CharacteristicDefinitions,
    CatalogImportNormalizedRowData Before,
    CatalogImportNormalizedRowData After);