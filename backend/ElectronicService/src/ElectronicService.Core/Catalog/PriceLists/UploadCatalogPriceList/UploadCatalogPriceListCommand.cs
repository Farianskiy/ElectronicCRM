namespace ElectronicService.Core.Catalog.PriceLists.UploadCatalogPriceList;

public sealed class UploadCatalogPriceListCommand
{
    public UploadCatalogPriceListCommand(
        Guid manufacturerId,
        Guid createdByUserId,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        ManufacturerId = manufacturerId;
        CreatedByUserId = createdByUserId;
        FileStream = fileStream;
        FileName = fileName;
        ContentType = contentType;
    }

    public Guid ManufacturerId
    {
        get;
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