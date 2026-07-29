namespace ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;

public sealed record ExportCatalogImportErrorReportQuery(
    Guid BatchId,
    Guid CurrentUserId);