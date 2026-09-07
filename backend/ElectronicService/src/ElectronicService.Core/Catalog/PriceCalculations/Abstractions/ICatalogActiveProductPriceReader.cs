namespace ElectronicService.Core.Catalog.PriceCalculations.Abstractions;

public interface ICatalogActiveProductPriceReader
{
    Task<IReadOnlyList<CatalogActiveProductPriceSource>>
        FindByProductIdAsync(
            Guid productId,
            int take,
            CancellationToken cancellationToken = default);
}

public sealed record CatalogActiveProductPriceSource(
    Guid ProductId,
    Guid ManufacturerId,
    string ManufacturerName,
    Guid PriceListId,
    Guid PriceListRowId,
    string Article,
    string Name,
    string? Unit,
    decimal BasePriceAmount,
    decimal? MrcPriceAmount);