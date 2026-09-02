using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportRecognitionShadowService
{
    Task<CatalogImportRecognitionShadowResult> AnalyzeAsync(CatalogImportWorkbookAnalysis analysis, ProductType productType, IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions, CancellationToken cancellationToken = default);
}