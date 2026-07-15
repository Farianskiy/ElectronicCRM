using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.TestCommon;

namespace ElectronicService.Domain.UnitTests.Catalog.Characteristics;

public sealed class CharacteristicDefinitionTests
{
    [Fact]
    public void CreateNormalizesCodeAndTrimsTextFields()
    {
        var result = CharacteristicDefinition.Create(
            " rated-current ",
            "  Номинальный ток  ",
            CharacteristicDataType.Number,
            "  А  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("RATED_CURRENT", result.Value.Code);
        Assert.Equal("Номинальный ток", result.Value.Name);
        Assert.Equal("А", result.Value.Unit);
    }

    [Fact]
    public void CreateReturnsInvalidErrorForNoneDataType()
    {
        var result = CharacteristicDefinition.Create(
            "CODE",
            "Название",
            CharacteristicDataType.None);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    [Fact]
    public void ValidateValueReturnsSuccessForMatchingType()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            dataType: CharacteristicDataType.Number);
        var value = CharacteristicValue.CreateNumber(16m).Value;

        var result = definition.ValidateValue(value);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateValueReturnsTypeMismatchForDifferentType()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            dataType: CharacteristicDataType.Number);
        var value = CharacteristicValue.CreateText("16").Value;

        var result = definition.ValidateValue(value);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.characteristic_value_type_mismatch",
            result.Error.Code);
    }

    [Fact]
    public void RenameChangesDefinitionName()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        var result = definition.Rename("  Ток автомата  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ток автомата", definition.Name);
    }

    [Fact]
    public void ChangeUnitConvertsBlankUnitToNull()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition();

        var result = definition.ChangeUnit(" ");

        Assert.True(result.IsSuccess);
        Assert.Null(definition.Unit);
    }

    [Fact]
    public void ChangeUnitDoesNotChangeUnitWhenNewUnitIsTooLong()
    {
        var definition = TestDataFactory.CreateCharacteristicDefinition(unit: "А");

        var result = definition.ChangeUnit(new string('U', 51));

        Assert.True(result.IsFailure);
        Assert.Equal("А", definition.Unit);
    }
}
