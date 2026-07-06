namespace ElectronicService.Infrastructure.Postgres.Catalog.Import;

internal sealed record ExcelImportProfile(
    string ProductTypeCode,
    string? DefaultCabinetKind,
    string? DefaultProductSeries,
    string NameColumn,
    string? ArticleColumn,
    string? CodeColumn,
    string ManufacturerColumn,
    IReadOnlyDictionary<string, string> CharacteristicColumnMap);