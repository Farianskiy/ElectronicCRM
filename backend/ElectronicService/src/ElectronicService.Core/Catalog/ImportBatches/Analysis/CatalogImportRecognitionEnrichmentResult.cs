using ElectronicService.Core.Catalog.Recognition.Effective;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRecognitionEnrichmentResult(
    CatalogImportWorkbookAnalysis Analysis,
    CatalogImportRecognitionEnrichmentSummary Summary,
    IReadOnlyDictionary<int, CatalogImportRowRecognition>? RecognitionResults = null);