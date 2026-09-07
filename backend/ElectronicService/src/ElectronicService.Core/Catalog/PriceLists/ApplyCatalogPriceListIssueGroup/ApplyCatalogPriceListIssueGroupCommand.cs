namespace ElectronicService.Core.Catalog.PriceLists.ApplyCatalogPriceListIssueGroup;

public sealed record ApplyCatalogPriceListIssueGroupCommand(
    Guid PriceListId,
    Guid CurrentUserId,
    string GroupKey,
    string? Unit,
    Guid? ProductId);