namespace ElectronicService.Core.Catalog.PriceLists.Import;

public sealed record CatalogPriceListWorkbookReadProgress(
    int EstimatedRowsCount,
    int ReadRowsCount);