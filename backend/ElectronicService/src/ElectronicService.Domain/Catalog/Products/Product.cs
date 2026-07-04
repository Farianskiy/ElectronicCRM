using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Products;

public sealed class Product : AggregateRoot
{
    private readonly List<ProductCharacteristic> _characteristics = [];
    private readonly List<ProductAlias> _aliases = [];

    private Product(
        Guid id,
        ProductArticle article,
        ProductName name,
        Guid productTypeId,
        Guid manufacturerId,
        Money price,
        StockQuantity stockQuantity)
        : base(id)
    {
        Article = article;
        Name = name;
        ProductTypeId = productTypeId;
        ManufacturerId = manufacturerId;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private Product()
    {
    }

    public ProductArticle Article { get; private set; } = null!;

    public ProductName Name { get; private set; } = null!;

    public Guid ProductTypeId { get; private set; }

    public Guid ManufacturerId { get; private set; }

    public Money Price { get; private set; } = null!;

    public StockQuantity StockQuantity { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ProductCharacteristic> Characteristics => _characteristics;

    public IReadOnlyCollection<ProductAlias> Aliases => _aliases;

    public bool IsAvailable => StockQuantity.IsAvailable;

    public static Result<Product, DomainError> Create(
        string article,
        string name,
        Guid productTypeId,
        Guid manufacturerId,
        Money price,
        StockQuantity stockQuantity)
    {
        var articleResult = ProductArticle.Create(article);

        if (articleResult.IsFailure)
        {
            return articleResult.Error;
        }

        var nameResult = ProductName.Create(name);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        if (productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (manufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        return new Product(
            Guid.CreateVersion7(),
            articleResult.Value,
            nameResult.Value,
            productTypeId,
            manufacturerId,
            price,
            stockQuantity);
    }

    // Метод меняет название товара
    public UnitResult<DomainError> Rename(string name)
    {
        var nameResult = ProductName.Create(name);

        if (nameResult.IsFailure)
        {
            return UnitResult.Failure(nameResult.Error);
        }

        Name = nameResult.Value;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод меняет артикул товара
    public UnitResult<DomainError> ChangeArticle(string article)
    {
        var articleResult = ProductArticle.Create(article);

        if (articleResult.IsFailure)
        {
            return UnitResult.Failure(articleResult.Error);
        }

        Article = articleResult.Value;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод меняет цену товара
    public UnitResult<DomainError> ChangePrice(Money price)
    {
        Price = price;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод меняет количество товара на складе
    public UnitResult<DomainError> ChangeStockQuantity(StockQuantity stockQuantity)
    {
        StockQuantity = stockQuantity;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод добавляет или обновляет характеристику товара
    public UnitResult<DomainError> SetCharacteristic(
        ProductType productType,
        CharacteristicDefinition definition,
        CharacteristicValue value)
    {
        if (productType.Id != ProductTypeId)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(productType)));
        }

        if (!productType.AllowsCharacteristic(definition.Id))
        {
            return UnitResult.Failure(
                CatalogErrors.CharacteristicIsNotAllowedForProductType(
                    definition.Id,
                    productType.Id));
        }

        var valueValidationResult = definition.ValidateValue(value);

        if (valueValidationResult.IsFailure)
        {
            return UnitResult.Failure(valueValidationResult.Error);
        }

        var existingCharacteristic = _characteristics
            .FirstOrDefault(x => x.CharacteristicDefinitionId == definition.Id);

        // Если такая характеристика уже есть у товара, то обновляем её значение
        if (existingCharacteristic is not null)
        {
            existingCharacteristic.ChangeValue(value);
            UpdatedAtUtc = DateTime.UtcNow;

            return UnitResult.Success<DomainError>();
        }

        // Если такой характеристики ещё нет у товара, то создаём новую
        var characteristicResult = ProductCharacteristic.Create(
            Id,
            definition.Id,
            value);

        if (characteristicResult.IsFailure)
        {
            return UnitResult.Failure(characteristicResult.Error);
        }

        _characteristics.Add(characteristicResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод удаляет характеристику товара
    public UnitResult<DomainError> RemoveCharacteristic(
    ProductType productType,
    Guid characteristicDefinitionId)
    {
        if (productType.Id != ProductTypeId)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(productType)));
        }

        if (productType.IsCharacteristicRequired(characteristicDefinitionId))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId)));
        }

        var characteristic = _characteristics
            .FirstOrDefault(x => x.CharacteristicDefinitionId == characteristicDefinitionId);

        if (characteristic is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductDoesNotHaveCharacteristic(characteristicDefinitionId));
        }

        _characteristics.Remove(characteristic);
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод добавляет альтернативное название товара (алиас)
    public UnitResult<DomainError> AddAlias(string alias)
    {
        var aliasResult = ProductAlias.Create(Id, alias);

        if (aliasResult.IsFailure)
        {
            return UnitResult.Failure(aliasResult.Error);
        }

        var alreadyExists = _aliases.Any(x =>
            string.Equals(
                x.NormalizedValue,
                aliasResult.Value.NormalizedValue,
                StringComparison.Ordinal));

        if (alreadyExists)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductAliasAlreadyExists(alias));
        }

        _aliases.Add(aliasResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    // Метод проверяет, заполнены ли все обязательные характеристики товар
    public UnitResult<DomainError> ValidateRequiredCharacteristics(ProductType productType)
    {
        if (productType.Id != ProductTypeId)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(productType)));
        }

        var productCharacteristicIds = _characteristics
            .Select(x => x.CharacteristicDefinitionId)
            .ToHashSet();

        var missingRequiredCharacteristicId = productType
            .FindMissingRequiredCharacteristicId(productCharacteristicIds);

        if (missingRequiredCharacteristicId is not null && missingRequiredCharacteristicId != Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.RequiredCharacteristicIsMissing(
                    missingRequiredCharacteristicId.Value.ToString()));
        }

        return UnitResult.Success<DomainError>();
    }
}