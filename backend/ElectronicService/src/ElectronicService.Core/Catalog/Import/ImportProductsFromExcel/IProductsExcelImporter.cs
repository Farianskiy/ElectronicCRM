using ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;

namespace ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;

public interface IProductsExcelImporter
{
    Task<ImportProductsFromExcelResult> ImportAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<PreviewProductsExcelImportResult> PreviewAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}