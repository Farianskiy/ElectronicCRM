using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Infrastructure.Postgres.Catalog.Components;

namespace ElectronicService.Infrastructure.UnitTests.Catalog.Components;

public sealed class ComponentCompatibilityCharacteristicValueConverterTests
{
    [Theory]
    [InlineData("1", "1P")]
    [InlineData("2", "2P")]
    [InlineData("3", "3P")]
    [InlineData("4", "4P")]
    [InlineData("3P", "3P")]
    [InlineData("3п + н", "3P+N")]
    public void ParseNormalizesPoleValues(string rawValue, string expected)
    {
        var definition = CreateDefinition(
            "POLES",
            CharacteristicDataType.Text);

        var result = ComponentCompatibilityCharacteristicValueConverter.Parse(
            definition,
            rawValue);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.TextValue);
    }

    [Theory]
    [InlineData("1P", "1")]
    [InlineData("2P", "2")]
    [InlineData("3P", "3")]
    [InlineData("4P", "4")]
    [InlineData("3P+N", "3P+N")]
    public void FormatForExcelUsesDigitsForSimplePoleValues(
        string storedValue,
        string expected)
    {
        var definition = CreateDefinition(
            "POLES",
            CharacteristicDataType.Text);
        var value = ComponentCompatibilityCharacteristicValueConverter.Parse(
            definition,
            storedValue).Value;

        var result = ComponentCompatibilityCharacteristicValueConverter
            .FormatForExcel(definition.Code, value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseRejectsUnknownPoleValue()
    {
        var definition = CreateDefinition(
            "POLES",
            CharacteristicDataType.Text);

        var result = ComponentCompatibilityCharacteristicValueConverter.Parse(
            definition,
            "5");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ParseDoesNotChangeOrdinaryTextCharacteristics()
    {
        var definition = CreateDefinition(
            "PRODUCT_SERIES",
            CharacteristicDataType.Text);

        var result = ComponentCompatibilityCharacteristicValueConverter.Parse(
            definition,
            "  ВА-335E  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("ВА-335E", result.Value.TextValue);
    }

    private static CharacteristicDefinition CreateDefinition(
        string code,
        CharacteristicDataType dataType)
    {
        return CharacteristicDefinition.Create(
            code,
            code,
            dataType).Value;
    }
}
