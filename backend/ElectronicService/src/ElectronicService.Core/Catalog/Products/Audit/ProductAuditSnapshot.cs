namespace ElectronicService.Core.Catalog.Products.Audit;

public sealed record ProductAuditSnapshot(
    Guid ProductId,
    string Article,
    string Name,
    Guid ProductTypeId,
    Guid ManufacturerId,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<
        ProductAuditCharacteristicSnapshot>
        Characteristics,
    IReadOnlyCollection<
        ProductAuditAliasSnapshot>
        Aliases);

public sealed record ProductAuditCharacteristicSnapshot(
    Guid DefinitionId,
    string DataType,
    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue);

public sealed record ProductAuditAliasSnapshot(
    Guid AliasId,
    string Value);