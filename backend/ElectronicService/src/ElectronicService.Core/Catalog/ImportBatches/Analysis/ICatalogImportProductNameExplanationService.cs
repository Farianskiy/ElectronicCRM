namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportProductNameExplanationService
{
    CatalogImportProductNameExplanationSummary Analyze(
        CatalogImportWorkbookAnalysis analysis,
        CatalogImportManufacturerRecognitionShadowResult manufacturerRecognitionShadow,
        CatalogImportProductTypeSuggestionShadowResult productTypeSuggestionShadow,
        CatalogImportRecognitionShadowResult? characteristicRecognitionShadow,
        CancellationToken cancellationToken = default);
}