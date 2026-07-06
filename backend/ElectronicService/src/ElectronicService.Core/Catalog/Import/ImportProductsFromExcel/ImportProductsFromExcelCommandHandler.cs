namespace ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;

public sealed class ImportProductsFromExcelCommandHandler
{
    private readonly IProductsExcelImporter _productsExcelImporter;

    public ImportProductsFromExcelCommandHandler(
        IProductsExcelImporter productsExcelImporter)
    {
        _productsExcelImporter = productsExcelImporter;
    }

    public Task<ImportProductsFromExcelResult> Handle(
    ImportProductsFromExcelCommand command,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return _productsExcelImporter.ImportAsync(
            command.FileStream,
            command.FileName,
            cancellationToken);
    }
}