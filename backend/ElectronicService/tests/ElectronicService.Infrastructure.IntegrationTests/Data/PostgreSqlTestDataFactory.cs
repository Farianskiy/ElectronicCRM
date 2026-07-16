using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.TestCommon;

namespace ElectronicService.Infrastructure.IntegrationTests.Data;

/// <summary>
/// Создаёт уникальный связанный набор данных каталога,
/// пригодный для сохранения в настоящую PostgreSQL.
/// Уникальный suffix предотвращает конфликты между тестами.
/// </summary>
internal static class PostgreSqlTestDataFactory
{
    public static CatalogProductGraph CreateCatalogProductGraph()
    {
        var suffix = Guid.NewGuid().ToString("N");

        var manufacturer = CreateManufacturer(
            $"Test Manufacturer {suffix}");

        var definition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"RATED_CURRENT_{suffix}",
                name: $"Номинальный ток {suffix}",
                dataType: CharacteristicDataType.Number,
                unit: "А");

        var productType = TestDataFactory.CreateProductType(
            code: $"MCB_{suffix}",
            name: $"Модульный автомат {suffix}");

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true,
            isFilterable: true,
            isUsedForReplacement: true,
            replacementMatchMode: ReplacementMatchMode.Exact,
            replacementWeight: 100);

        var product = TestDataFactory.CreateProduct(
            article: $"TEST-{suffix}",
            name: $"Тестовый автомат {suffix}",
            productTypeId: productType.Id,
            manufacturerId: manufacturer.Id,
            price: 1_234.56m,
            stockQuantity: 7.125m);

        var characteristicResult = product.SetCharacteristic(
            productType,
            definition,
            TestDataFactory.CreateNumberValue(16.5m));

        if (characteristicResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось установить тестовую характеристику: " +
                $"{characteristicResult.Error.Code}: " +
                $"{characteristicResult.Error.Message}");
        }

        var aliasResult = product.AddAlias(
            $"Альтернативное название {suffix}");

        if (aliasResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось добавить тестовый алиас: " +
                $"{aliasResult.Error.Code}: " +
                $"{aliasResult.Error.Message}");
        }

        return new CatalogProductGraph(
            manufacturer,
            productType,
            definition,
            product);
    }

    public static Manufacturer CreateManufacturer(string name)
    {
        var result = Manufacturer.Create(name);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось создать тестового производителя: " +
                $"{result.Error.Code}: {result.Error.Message}");
        }

        return result.Value;
    }
}

/// <summary>
/// Связанный граф сущностей, необходимый для сохранения Product.
/// </summary>
public sealed record CatalogProductGraph(
    Manufacturer Manufacturer,
    ProductType ProductType,
    CharacteristicDefinition Definition,
    Product Product);
