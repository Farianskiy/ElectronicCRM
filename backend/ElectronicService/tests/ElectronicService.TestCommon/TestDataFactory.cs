using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;

namespace ElectronicService.TestCommon;

/// <summary>
/// Создаёт заведомо валидные доменные объекты для тестов.
/// Если production-инвариант изменится, фабрика завершится понятной ошибкой,
/// а не отдаст частично созданный объект.
/// </summary>
public static class TestDataFactory
{
    public static Money CreateMoney(
        decimal amount = 1_000m,
        string currency = "RUB")
    {
        return GetValue(Money.Create(amount, currency));
    }

    public static StockQuantity CreateStockQuantity(decimal value = 10m)
    {
        return GetValue(StockQuantity.Create(value));
    }

    public static CharacteristicValue CreateTextValue(string value = "C")
    {
        return GetValue(CharacteristicValue.CreateText(value));
    }

    public static CharacteristicValue CreateNumberValue(decimal value = 16m)
    {
        return GetValue(CharacteristicValue.CreateNumber(value));
    }

    public static CharacteristicValue CreateBooleanValue(bool value = true)
    {
        return GetValue(CharacteristicValue.CreateBoolean(value));
    }

    public static CharacteristicDefinition CreateCharacteristicDefinition(
        string code = "RATED_CURRENT",
        string name = "Номинальный ток",
        CharacteristicDataType dataType = CharacteristicDataType.Number,
        string? unit = "А")
    {
        return GetValue(
            CharacteristicDefinition.Create(
                code,
                name,
                dataType,
                unit));
    }

    public static ProductType CreateProductType(
        string code = "MODULAR_CIRCUIT_BREAKER",
        string name = "Модульный автомат")
    {
        return GetValue(ProductType.Create(code, name));
    }

    public static Product CreateProduct(
        string article = "TEST-ARTICLE-001",
        string name = "Автоматический выключатель",
        Guid? productTypeId = null,
        Guid? manufacturerId = null,
        decimal price = 1_000m,
        decimal stockQuantity = 10m)
    {
        return GetValue(
            Product.Create(
                article,
                name,
                productTypeId ?? Guid.NewGuid(),
                manufacturerId ?? Guid.NewGuid(),
                CreateMoney(price),
                CreateStockQuantity(stockQuantity)));
    }

    public static User CreateRegularUser(
        string displayName = "Обычный пользователь",
        string? email = "regular@example.com",
        string? passwordHash = "regular-password-hash")
    {
        return GetValue(
            User.CreateRegular(
                displayName,
                email,
                passwordHash));
    }

    public static User CreateTechnicalUser(
        string displayName = "Технический пользователь",
        string email = "technical@example.com",
        string? passwordHash = "technical-password-hash")
    {
        return GetValue(
            User.CreateTechnical(
                displayName,
                email,
                passwordHash));
    }

    public static void AddCharacteristic(
        ProductType productType,
        CharacteristicDefinition definition,
        bool isRequired = false,
        bool isFilterable = true,
        bool isUsedForReplacement = true,
        ReplacementMatchMode replacementMatchMode = ReplacementMatchMode.Exact,
        int replacementWeight = 100)
    {
        var result = productType.AddCharacteristic(
            definition,
            isRequired,
            isFilterable,
            isUsedForReplacement,
            replacementMatchMode,
            replacementWeight);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось добавить тестовую характеристику: " +
                $"{result.Error.Code}: {result.Error.Message}");
        }
    }

    private static T GetValue<T>(Result<T, DomainError> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось создать тестовые данные: " +
                $"{result.Error.Code}: {result.Error.Message}");
        }

        return result.Value;
    }
}
