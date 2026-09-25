namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportRecognitionShadowService
{
    CatalogImportRecognitionShadowResult Analyze(
        CatalogImportWorkbookAnalysis analysis,
        IReadOnlyDictionary<int, CatalogImportRowRecognition> recognitionResults,
        CancellationToken cancellationToken = default);
}
