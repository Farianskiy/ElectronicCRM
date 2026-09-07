namespace ElectronicService.Domain.Catalog.PriceLists;

public sealed record CatalogPriceListRowIssue(
    string Code,
    string Field,
    string Message);