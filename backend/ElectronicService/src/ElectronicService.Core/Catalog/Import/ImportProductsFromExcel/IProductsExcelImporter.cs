namespace ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;

public interface IProductsExcelImporter
{
    Task<ImportProductsFromExcelResult> ImportAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}