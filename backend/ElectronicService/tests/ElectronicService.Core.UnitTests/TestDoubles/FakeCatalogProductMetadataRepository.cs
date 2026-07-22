using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.UnitTests.TestDoubles;

/// <summary>
/// Тестовая реализация репозитория метаданных каталога.
/// Возвращает типы товаров, производителей и определения
/// характеристик из памяти, а также записывает параметры вызовов.
/// </summary>
internal sealed class FakeCatalogProductMetadataRepository
    : ICatalogProductMetadataRepository
{
    private readonly Dictionary<Guid, ProductType>
        _productTypes = [];

    private readonly Dictionary<
        string,
        CharacteristicDefinition>
        _definitionsByCode =
            new(StringComparer.Ordinal);

    private readonly Dictionary<
        Guid,
        CharacteristicDefinition>
        _definitionsById = [];

    private readonly Dictionary<Guid, Manufacturer>
        _manufacturers = [];

    public int GetProductTypeByIdCallsCount
    {
        get;
        private set;
    }

    public int GetCharacteristicDefinitionByCodeCallsCount
    {
        get;
        private set;
    }

    public int GetCharacteristicDefinitionsByIdsCallsCount
    {
        get;
        private set;
    }

    public int GetManufacturerByIdCallsCount
    {
        get;
        private set;
    }

    public Guid? LastProductTypeId
    {
        get;
        private set;
    }

    public string? LastCharacteristicCode
    {
        get;
        private set;
    }

    public IReadOnlyCollection<Guid>?
        LastCharacteristicDefinitionIds
    {
        get;
        private set;
    }

    public Guid? LastManufacturerId
    {
        get;
        private set;
    }

    public CancellationToken
        LastProductTypeCancellationToken
    {
        get;
        private set;
    }

    public CancellationToken
        LastDefinitionCancellationToken
    {
        get;
        private set;
    }

    public CancellationToken
        LastDefinitionsCancellationToken
    {
        get;
        private set;
    }

    public CancellationToken
        LastManufacturerCancellationToken
    {
        get;
        private set;
    }

    /// <summary>
    /// Регистрирует тип товара по его идентификатору.
    /// </summary>
    public void AddExisting(
        ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(
            productType);

        _productTypes[productType.Id] =
            productType;
    }

    /// <summary>
    /// Регистрирует definition одновременно
    /// по коду и по идентификатору.
    /// </summary>
    public void AddExisting(
        CharacteristicDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        _definitionsByCode[
            NormalizeCode(definition.Code)] =
                definition;

        _definitionsById[definition.Id] =
            definition;
    }

    /// <summary>
    /// Регистрирует производителя.
    /// </summary>
    public void AddExisting(
        Manufacturer manufacturer)
    {
        ArgumentNullException.ThrowIfNull(
            manufacturer);

        _manufacturers[manufacturer.Id] =
            manufacturer;
    }

    /// <summary>
    /// Регистрирует тип товара под заданным
    /// ключом поиска.
    ///
    /// Используется для проверки ситуации,
    /// когда repository возвращает тип с Id,
    /// не совпадающим с ключом поиска.
    /// </summary>
    public void AddProductTypeForLookup(
        Guid lookupId,
        ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(
            productType);

        _productTypes[lookupId] =
            productType;
    }

    public Task<ProductType?>
        GetProductTypeByIdAsync(
            Guid productTypeId,
            CancellationToken cancellationToken = default)
    {
        GetProductTypeByIdCallsCount++;

        LastProductTypeId =
            productTypeId;

        LastProductTypeCancellationToken =
            cancellationToken;

        _productTypes.TryGetValue(
            productTypeId,
            out var productType);

        return Task.FromResult<ProductType?>(
            productType);
    }

    public Task<CharacteristicDefinition?>
        GetCharacteristicDefinitionByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
    {
        GetCharacteristicDefinitionByCodeCallsCount++;

        LastCharacteristicCode =
            code;

        LastDefinitionCancellationToken =
            cancellationToken;

        _definitionsByCode.TryGetValue(
            NormalizeCode(code),
            out var definition);

        return Task.FromResult<
            CharacteristicDefinition?>(
                definition);
    }

    public Task<IReadOnlyCollection<
            CharacteristicDefinition>>
        GetCharacteristicDefinitionsByIdsAsync(
            IReadOnlyCollection<Guid>
                characteristicDefinitionIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            characteristicDefinitionIds);

        GetCharacteristicDefinitionsByIdsCallsCount++;

        LastCharacteristicDefinitionIds =
            characteristicDefinitionIds.ToArray();

        LastDefinitionsCancellationToken =
            cancellationToken;

        IReadOnlyCollection<CharacteristicDefinition>
            definitions = characteristicDefinitionIds
                .Distinct()
                .Where(definitionId =>
                    _definitionsById.ContainsKey(
                        definitionId))
                .Select(definitionId =>
                    _definitionsById[
                        definitionId])
                .ToList();

        return Task.FromResult(definitions);
    }

    public Task<Manufacturer?>
        GetManufacturerByIdAsync(
            Guid manufacturerId,
            CancellationToken cancellationToken = default)
    {
        GetManufacturerByIdCallsCount++;

        LastManufacturerId =
            manufacturerId;

        LastManufacturerCancellationToken =
            cancellationToken;

        _manufacturers.TryGetValue(
            manufacturerId,
            out var manufacturer);

        return Task.FromResult<Manufacturer?>(
            manufacturer);
    }

    private static string NormalizeCode(
        string code)
    {
        return code
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal);
    }
}