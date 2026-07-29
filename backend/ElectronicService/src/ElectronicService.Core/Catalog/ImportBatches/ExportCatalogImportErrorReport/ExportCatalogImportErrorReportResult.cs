namespace ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;

public sealed record ExportCatalogImportErrorReportResult(
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content);