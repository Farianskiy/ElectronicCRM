namespace ElectronicService.Core.Catalog.Products.RemoveAlias;

public sealed record RemoveProductAliasCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    Guid AliasId);