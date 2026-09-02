namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRecognitionEnrichmentResult(
    CatalogImportWorkbookAnalysis Analysis,
    CatalogImportRecognitionEnrichmentSummary Summary);