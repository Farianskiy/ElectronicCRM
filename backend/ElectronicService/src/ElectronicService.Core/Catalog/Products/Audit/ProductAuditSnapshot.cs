namespace ElectronicService.Core.Catalog.Products.Audit;

public static class ProductAuditSnapshotVersions
{
    /*
     * Старые записи, созданные до введения
     * SnapshotVersion, при десериализации
     * автоматически получат значение 0.
     */
    public const int Legacy = 0;

    public const int Current = 1;
}

public sealed record ProductAuditSnapshot(
    int SnapshotVersion,

    Guid ProductId,
    string Article,
    string Name,

    Guid ProductTypeId,
    string? ProductTypeCode,
    string? ProductTypeName,

    Guid ManufacturerId,
    string? ManufacturerName,

    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,

    IReadOnlyCollection<
        ProductAuditCharacteristicSnapshot>
        Characteristics,

    IReadOnlyCollection<
        ProductAuditAliasSnapshot>
        Aliases);

public sealed record ProductAuditCharacteristicSnapshot(
    Guid DefinitionId,

    /*
     * Для старых legacy-записей эти поля
     * будут null. Reader применит fallback
     * к текущему справочнику.
     */
    string? Code,
    string? Name,

    string DataType,
    string? Unit,

    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue);

public sealed record ProductAuditAliasSnapshot(
    Guid AliasId,
    string Value);