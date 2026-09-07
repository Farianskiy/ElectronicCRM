namespace ElectronicService.Core.Catalog.PriceLists.Import;

public sealed record CatalogPriceListWorkbookReadSummary(
    string WorksheetName,
    DateOnly EffectiveDate,
    int HeaderRowNumber,
    int RowsCount);