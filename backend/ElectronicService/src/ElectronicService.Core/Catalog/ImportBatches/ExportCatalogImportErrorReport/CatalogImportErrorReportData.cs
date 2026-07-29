using ElectronicService.Core.Catalog.ImportBatches.Analysis;

namespace ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;

public sealed record CatalogImportErrorReportData(
    Guid BatchId,
    string OriginalFileName,
    string Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime GeneratedAtUtc,
    IReadOnlyCollection<CatalogImportErrorReportColumn> Columns,
    IReadOnlyCollection<CatalogImportErrorReportRow> ErrorRows);

public sealed record CatalogImportErrorReportColumn(
    int SourceColumnNumber,
    string SourceHeader);

public sealed record CatalogImportErrorReportRow(
    int RowNumber,
    IReadOnlyDictionary<int, string> RawData,
    IReadOnlyCollection<CatalogImportRowIssue> Issues,
    IReadOnlyCollection<CatalogImportRowIssue> Warnings);