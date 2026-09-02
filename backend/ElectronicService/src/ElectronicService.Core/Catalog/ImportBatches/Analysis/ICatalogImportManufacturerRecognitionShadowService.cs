using ElectronicService.Core.Catalog.Manufacturers.Resolution;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportManufacturerRecognitionShadowService
{
    CatalogImportManufacturerRecognitionShadowResult Analyze(
        CatalogImportWorkbookAnalysis analysis,
        ManufacturerResolutionIndex manufacturerResolutionIndex,
        CancellationToken cancellationToken = default);
}