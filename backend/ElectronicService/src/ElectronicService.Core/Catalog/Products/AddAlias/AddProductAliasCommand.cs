namespace ElectronicService.Core.Catalog.Products.AddAlias;

public sealed record AddProductAliasCommand(
    Guid ProductId,
    string Alias);