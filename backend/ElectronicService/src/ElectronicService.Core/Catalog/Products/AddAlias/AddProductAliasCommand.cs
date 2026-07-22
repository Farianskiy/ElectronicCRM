namespace ElectronicService.Core.Catalog.Products.AddAlias;

public sealed record AddProductAliasCommand(
    Guid ProductId,
    Guid ChangedByUserId,
    string Alias);