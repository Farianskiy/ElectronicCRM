namespace ElectronicService.Core.Catalog.ImportBatches.CreateCatalogImportBatch;

public sealed class CreateCatalogImportBatchCommand
{
    public CreateCatalogImportBatchCommand(
        Guid createdByUserId,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(
            fileStream);

        CreatedByUserId = createdByUserId;
        FileStream = fileStream;
        FileName = fileName;
        ContentType = contentType;
    }

    public Guid CreatedByUserId
    {
        get;
    }

    public Stream FileStream
    {
        get;
    }

    public string FileName
    {
        get;
    }

    public string ContentType
    {
        get;
    }
}