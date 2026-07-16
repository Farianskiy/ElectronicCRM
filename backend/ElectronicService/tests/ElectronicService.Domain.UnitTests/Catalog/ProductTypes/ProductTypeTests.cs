using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.TestCommon;

namespace ElectronicService.Domain.UnitTests.Catalog.ProductTypes;

public sealed class ProductTypeTests
{
    // Проверяет создание типа товара с нормализованным кодом и очищенным названием.
    [Fact]
    public void CreateNormalizesCodeAndTrimsName()
    {
        var result = ProductType.Create(
            " modular-circuit breaker ",
            "  Модульный автомат  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("MODULAR_CIRCUIT_BREAKER", result.Value.Code);
        Assert.Equal("Модульный автомат", result.Value.Name);
    }

    // Проверяет сохранение всех настроек характеристики внутри типа товара.
    [Fact]
    public void AddCharacteristicStoresAllConfiguration()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        var result = productType.AddCharacteristic(
            definition,
            isRequired: true,
            isFilterable: true,
            isUsedForReplacement: true,
            replacementMatchMode: ReplacementMatchMode.GreaterOrEqual,
            replacementWeight: 150);

        Assert.True(result.IsSuccess);

        var relation = Assert.Single(productType.Characteristics);

        Assert.Equal(productType.Id, relation.ProductTypeId);
        Assert.Equal(definition.Id, relation.CharacteristicDefinitionId);
        Assert.True(relation.IsRequired);
        Assert.True(relation.IsFilterable);
        Assert.True(relation.IsUsedForReplacement);
        Assert.Equal(
            ReplacementMatchMode.GreaterOrEqual,
            relation.ReplacementMatchMode);
        Assert.Equal(150, relation.ReplacementWeight);
    }

    // Проверяет запрет повторного добавления одной характеристики к типу товара.
    [Fact]
    public void AddCharacteristicRejectsDuplicateDefinition()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        TestDataFactory.AddCharacteristic(productType, definition);

        var result = productType.AddCharacteristic(
            definition,
            isRequired: false,
            isFilterable: true,
            isUsedForReplacement: false,
            replacementMatchMode: ReplacementMatchMode.None,
            replacementWeight: 0);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.characteristic_already_added_to_product_type",
            result.Error.Code);
        Assert.Single(productType.Characteristics);
    }

    // Проверяет обязательность режима сравнения для характеристики, участвующей в подборе аналогов.
    [Fact]
    public void AddCharacteristicRejectsNoneModeWhenUsedForReplacement()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        var result = productType.AddCharacteristic(
            definition,
            isRequired: false,
            isFilterable: true,
            isUsedForReplacement: true,
            replacementMatchMode: ReplacementMatchMode.None,
            replacementWeight: 10);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Empty(productType.Characteristics);
    }

    // Проверяет запрет отрицательного веса характеристики при подборе аналогов.
    [Fact]
    public void AddCharacteristicRejectsNegativeReplacementWeight()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        var result = productType.AddCharacteristic(
            definition,
            isRequired: false,
            isFilterable: true,
            isUsedForReplacement: false,
            replacementMatchMode: ReplacementMatchMode.None,
            replacementWeight: -1);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет обнаружение отсутствующей обязательной характеристики.
    [Fact]
    public void RequiredCharacteristicCanBeFoundWhenMissing()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true);

        var missingId = productType.FindMissingRequiredCharacteristicId(
            new HashSet<Guid>());

        Assert.Equal(definition.Id, missingId);
        Assert.True(productType.AllowsCharacteristic(definition.Id));
        Assert.True(productType.IsCharacteristicRequired(definition.Id));
    }

    // Проверяет отсутствие ошибки, когда все обязательные характеристики присутствуют.
    [Fact]
    public void FindMissingRequiredCharacteristicReturnsNullWhenAllArePresent()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true);

        var existingIds = new HashSet<Guid>
        {
            definition.Id
        };

        var missingId = productType.FindMissingRequiredCharacteristicId(
            existingIds);

        Assert.Null(missingId);
    }

    // Проверяет изменение настроек участия характеристики в подборе аналогов.
    [Fact]
    public void ConfigureReplacementUpdatesRelation()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isUsedForReplacement: false,
            replacementMatchMode: ReplacementMatchMode.None,
            replacementWeight: 0);

        var relation = Assert.Single(productType.Characteristics);

        var result = relation.ConfigureReplacement(
            isUsedForReplacement: true,
            replacementMatchMode: ReplacementMatchMode.Optional,
            replacementWeight: 25);

        Assert.True(result.IsSuccess);
        Assert.True(relation.IsUsedForReplacement);
        Assert.Equal(
            ReplacementMatchMode.Optional,
            relation.ReplacementMatchMode);
        Assert.Equal(25, relation.ReplacementWeight);
    }
}
