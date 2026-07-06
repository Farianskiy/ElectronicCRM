namespace ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;

public sealed class ImportProductsFromExcelCommand
{
    public ImportProductsFromExcelCommand(
        Stream fileStream,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName));
        }

        FileStream = fileStream;
        FileName = fileName;
    }

    public Stream FileStream { get; }

    public string FileName { get; }
}