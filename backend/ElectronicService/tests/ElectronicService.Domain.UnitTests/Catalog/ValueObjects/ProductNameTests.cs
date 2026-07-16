using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Catalog.ValueObjects;

public sealed class ProductNameTests
{
    // Проверяет очистку названия товара и построение нормализованного значения.
    [Fact]
    public void CreateTrimsAndNormalizesValidName()
    {
        var result = ProductName.Create("  Автомат ёлка  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Автомат ёлка", result.Value.Value);
        Assert.Equal("АВТОМАТ ЕЛКА", result.Value.NormalizedValue);
    }

    // Проверяет, что пустое название товара отклоняется.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateReturnsRequiredErrorForBlankName(string name)
    {
        var result = ProductName.Create(name);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    // Проверяет минимальную длину названия товара.
    [Fact]
    public void CreateReturnsTooShortErrorForOneCharacter()
    {
        var result = ProductName.Create(" A ");

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_short", result.Error.Code);
    }

    // Проверяет максимальную длину названия товара.
    [Fact]
    public void CreateReturnsTooLongErrorWhenNameExceedsLimit()
    {
        var result = ProductName.Create(new string('N', 501));

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_long", result.Error.Code);
    }

    // Проверяет сравнение названий по нормализованному значению без учёта регистра и различия Ё/Е.
    [Fact]
    public void EqualityUsesNormalizedValue()
    {
        var first = ProductName.Create("Автомат Ёлка").Value;
        var second = ProductName.Create(" автомат елка ").Value;

        Assert.Equal(first, second);
    }
}
