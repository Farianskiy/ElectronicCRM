namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record ApplyCatalogPriceListIssueGroupRequest(
    string? Unit,
    Guid? ProductId);