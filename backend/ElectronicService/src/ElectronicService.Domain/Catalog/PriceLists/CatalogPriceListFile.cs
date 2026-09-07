using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceLists;

public sealed class CatalogPriceListFile
{
    private byte[] ContentBytes
    {
        get;
        set;
    } = [];

    private CatalogPriceListFile(
        Guid priceListId,
        byte[] content)
    {
        PriceListId = priceListId;
        ContentBytes = content.ToArray();
    }

    private CatalogPriceListFile()
    {
    }

    public Guid PriceListId
    {
        get;
        private set;
    }

    public ReadOnlyMemory<byte> Content =>
        ContentBytes;

    internal static Result<CatalogPriceListFile, DomainError> Create(
        Guid priceListId,
        byte[] content)
    {
        if (priceListId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(priceListId));
        }

        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            return CatalogPriceListErrors.FileIsEmpty();
        }

        return new CatalogPriceListFile(
            priceListId,
            content);
    }
}