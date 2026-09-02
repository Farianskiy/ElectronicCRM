namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed record CatalogImportRecognitionFeedbackCollectionResult(
    CatalogImportNormalizedRowData Data,
    int CreatedFeedbackCount,
    int UpdatedFeedbackCount,
    int RemovedFeedbackCount);