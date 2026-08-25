namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record AnalyzeCatalogImportBatchResponse(
    Guid BatchId,
    string Status,
    Guid? ProductTypeId,
    int ColumnsCount,
    int UnmappedColumnsCount,
    int UnconfirmedColumnsCount,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    CatalogImportManufacturerResolutionSummaryResponse ManufacturerResolutionSummary,
    CatalogImportRecognitionShadowResponse? RecognitionShadow,
    CatalogImportRecognitionEnrichmentResponse RecognitionEnrichment);