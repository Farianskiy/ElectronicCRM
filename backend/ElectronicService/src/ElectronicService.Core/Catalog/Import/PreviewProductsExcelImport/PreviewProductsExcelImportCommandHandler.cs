using ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;

namespace ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;

public sealed class PreviewProductsExcelImportCommandHandler
{
    private readonly IProductsExcelImporter _productsExcelImporter;

    public PreviewProductsExcelImportCommandHandler(
        IProductsExcelImporter productsExcelImporter)
    {
        _productsExcelImporter = productsExcelImporter;
    }

    public Task<PreviewProductsExcelImportResult> Handle(
        PreviewProductsExcelImportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return _productsExcelImporter.PreviewAsync(
            command.FileStream,
            command.FileName,
            cancellationToken);
    }
}