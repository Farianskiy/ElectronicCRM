using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.UnitTests.TestDoubles;

/// <summary>
/// Тестовая реализация репозитория метаданных каталога.
/// Возвращает типы товаров и определения характеристик из памяти,
/// а также записывает параметры всех вызовов.
/// </summary>
internal sealed class FakeCatalogProductMetadataRepository
    : ICatalogProductMetadataRepository
{
    private readonly Dictionary<Guid, ProductType> _productTypes = [];

    private readonly Dictionary<string, CharacteristicDefinition>
        _definitionsByCode = new(StringComparer.Ordinal);

    public int GetProductTypeByIdCallsCount { get; private set; }

    public int GetCharacteristicDefinitionByCodeCallsCount { get; private set; }

    public Guid? LastProductTypeId { get; private set; }

    public string? LastCharacteristicCode { get; private set; }

    public CancellationToken LastProductTypeCancellationToken { get; private set; }

    public CancellationToken LastDefinitionCancellationToken { get; private set; }

    /// <summary>
    /// Регистрирует тип товара по его собственному идентификатору.
    /// </summary>
    public void AddExisting(ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(productType);

        _productTypes[productType.Id] = productType;
    }

    /// <summary>
    /// Регистрирует определение характеристики по нормализованному коду.
    /// </summary>
    public void AddExisting(CharacteristicDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _definitionsByCode[NormalizeCode(definition.Code)] = definition;
    }

    /// <summary>
    /// Регистрирует тип товара под заданным ключом поиска.
    /// Метод нужен для проверки ситуации, когда repository по ошибке
    /// возвращает тип с идентификатором, не совпадающим с типом товара.
    /// </summary>
    public void AddProductTypeForLookup(
        Guid lookupId,
        ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(productType);

        _productTypes[lookupId] = productType;
    }

    public Task<ProductType?> GetProductTypeByIdAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        GetProductTypeByIdCallsCount++;
        LastProductTypeId = productTypeId;
        LastProductTypeCancellationToken = cancellationToken;

        _productTypes.TryGetValue(productTypeId, out var productType);

        return Task.FromResult<ProductType?>(productType);
    }

    public Task<CharacteristicDefinition?>
        GetCharacteristicDefinitionByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
    {
        GetCharacteristicDefinitionByCodeCallsCount++;
        LastCharacteristicCode = code;
        LastDefinitionCancellationToken = cancellationToken;

        _definitionsByCode.TryGetValue(
            NormalizeCode(code),
            out var definition);

        return Task.FromResult<CharacteristicDefinition?>(definition);
    }

    private static string NormalizeCode(string code)
    {
        return code
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}