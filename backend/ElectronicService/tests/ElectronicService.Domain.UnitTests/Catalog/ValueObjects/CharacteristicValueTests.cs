using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Catalog.ValueObjects;

public sealed class CharacteristicValueTests
{
    [Fact]
    public void CreateTextTrimsValueAndSetsTextType()
    {
        var result = CharacteristicValue.CreateText("  C  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(CharacteristicDataType.Text, result.Value.DataType);
        Assert.Equal("C", result.Value.TextValue);
        Assert.Equal("C", result.Value.RawValue);
        Assert.Equal("C", result.Value.ToString());
    }

    [Fact]
    public void CreateTextReturnsRequiredErrorForBlankValue()
    {
        var result = CharacteristicValue.CreateText(" ");

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    [Fact]
    public void CreateNumberSetsOnlyNumberValue()
    {
        var result = CharacteristicValue.CreateNumber(16.5m);

        Assert.True(result.IsSuccess);
        Assert.Equal(CharacteristicDataType.Number, result.Value.DataType);
        Assert.Equal(16.5m, result.Value.NumberValue);
        Assert.Null(result.Value.TextValue);
        Assert.Null(result.Value.BooleanValue);
        Assert.Equal(16.5m, Assert.IsType<decimal>(result.Value.RawValue));
        Assert.Equal("16.5", result.Value.ToString());
    }

    [Fact]
    public void CreateBooleanSetsOnlyBooleanValue()
    {
        var result = CharacteristicValue.CreateBoolean(true);

        Assert.True(result.IsSuccess);
        Assert.Equal(CharacteristicDataType.Boolean, result.Value.DataType);
        Assert.True(result.Value.BooleanValue is true);
        Assert.Null(result.Value.TextValue);
        Assert.Null(result.Value.NumberValue);
        Assert.True(Assert.IsType<bool>(result.Value.RawValue));
        Assert.Equal("True", result.Value.ToString());
    }

    [Fact]
    public void TextEqualityIgnoresLetterCase()
    {
        var first = CharacteristicValue.CreateText("c").Value;
        var second = CharacteristicValue.CreateText("C").Value;

        Assert.Equal(first, second);
    }
}
