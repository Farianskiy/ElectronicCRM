namespace ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;

public interface ICatalogImportErrorReportGenerator
{
    byte[] Generate(
        CatalogImportErrorReportData data);
}