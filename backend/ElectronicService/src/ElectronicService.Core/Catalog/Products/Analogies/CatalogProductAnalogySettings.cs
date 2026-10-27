using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public enum CatalogProductAnalogyMode
{
    Off = 0,
    Shadow = 1,
    Suggest = 2,
    AutoFill = 3
}

public sealed record CatalogProductAnalogyCharacteristicSettings(
    CatalogProductAnalogyMode Mode,
    int? MinimumSupportingProducts = null,
    decimal? MinimumSupportRatio = null);

public sealed record CatalogProductAnalogySettings(
    int MinimumSupportingProducts,
    int MinimumProductsWithCharacteristic,
    decimal MinimumSupportRatio,
    int MaximumAlternativeValues,
    int MaximumCandidatePoolSize,
    IReadOnlyDictionary<string, CatalogProductAnalogyCharacteristicSettings> Characteristics)
{
    public static CatalogProductAnalogySettings SystemDefault { get; } = new(
        MinimumSupportingProducts: 3,
        MinimumProductsWithCharacteristic: 3,
        MinimumSupportRatio: 0.80m,
        MaximumAlternativeValues: 3,
        MaximumCandidatePoolSize: 2000,
        new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(
            StringComparer.Ordinal));

    public CatalogProductAnalogyCharacteristicSettings Resolve(string code)
    {
        var normalized = NormalizeCode(code);
        return Characteristics.TryGetValue(normalized, out var settings)
            ? settings
            : new CatalogProductAnalogyCharacteristicSettings(
                CatalogProductAnalogyMode.Suggest);
    }

    public static string NormalizeCode(string value) =>
        value.Trim().ToUpperInvariant();
}

public sealed record CatalogProductAnalogyConfigurationSnapshot(
    Guid? ConfigurationId,
    int? VersionNumber,
    string Name,
    string SettingsHash,
    bool IsSystem,
    CatalogProductAnalogySettings Settings);

public static class CatalogProductAnalogySettingsCodec
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static Result<(CatalogProductAnalogySettings Settings, string Json, string Hash), DomainError>
        Normalize(CatalogProductAnalogySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.MinimumSupportingProducts is < 1 or > 1000
            || settings.MinimumProductsWithCharacteristic is < 1 or > 1000
            || settings.MinimumSupportRatio is <= 0m or > 1m
            || settings.MaximumAlternativeValues is < 1 or > 100
            || settings.MaximumCandidatePoolSize is < 100 or > 10000)
        {
            return new DomainError(
                "catalog_analogy.invalid_settings",
                "Проверьте пороги и размер кандидатного пула.");
        }

        var characteristics = new SortedDictionary<string, CatalogProductAnalogyCharacteristicSettings>(
            StringComparer.Ordinal);
        foreach (var item in settings.Characteristics)
        {
            if (string.IsNullOrWhiteSpace(item.Key)
                || item.Value is null
                || !Enum.IsDefined(item.Value.Mode)
                || item.Value.MinimumSupportingProducts is < 1 or > 1000
                || item.Value.MinimumSupportRatio is <= 0m or > 1m)
            {
                return new DomainError(
                    "catalog_analogy.invalid_settings",
                    "Настройки характеристики содержат недопустимое значение.");
            }

            var code = CatalogProductAnalogySettings.NormalizeCode(item.Key);
            if (!characteristics.TryAdd(code, item.Value))
            {
                return new DomainError(
                    "catalog_analogy.invalid_settings",
                    "Характеристика не должна повторяться в конфигурации.");
            }
        }

        var normalized = settings with { Characteristics = characteristics };
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return (normalized, json, hash);
    }

    public static Result<CatalogProductAnalogySettings, DomainError> Deserialize(
        string json)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<CatalogProductAnalogySettings>(
                json,
                JsonOptions);
            if (settings is null)
            {
                throw new JsonException("Пустая конфигурация.");
            }

            var normalized = Normalize(settings);
            return normalized.IsSuccess
                ? normalized.Value.Settings
                : normalized.Error;
        }
        catch (JsonException)
        {
            return new DomainError(
                "catalog_analogy.invalid_settings",
                "Сохранённая конфигурация аналогов повреждена.");
        }
    }
}
