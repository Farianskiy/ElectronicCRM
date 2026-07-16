using ElectronicService.Infrastructure.Postgres.Catalog.Import;

namespace ElectronicService.Infrastructure.UnitTests.Catalog.Import;

public sealed class ManufacturerImportNormalizerTests
{
    // Проверяет, что пустое или отсутствующее название производителя заменяется на «Не указан».
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NormalizeManufacturerNameMapsBlankValueToUnknown(
        string? rawName)
    {
        // Act
        var result =
            ManufacturerImportNormalizer.NormalizeManufacturerName(rawName);

        // Assert
        Assert.Equal("Не указан", result.NormalizedName);
        Assert.True(result.WasChanged);
    }

    // Проверяет все утверждённые алиасы производителей и ожидаемое каноническое название.
    [Theory]
    // CHINT
    [InlineData("CHINT", "CHINT", false)]
    [InlineData("chint", "CHINT", true)]
    [InlineData(" ЧИНТ ", "CHINT", true)]
    [InlineData("ЧЕНТ", "CHINT", true)]
    [InlineData("ЧНТ", "CHINT", true)]
    [InlineData("ЧАНТ", "CHINT", true)]
    [InlineData("CHINT пром серия", "CHINT", true)]

    // IEK
    [InlineData("IEK", "IEK", false)]
    [InlineData("iek", "IEK", true)]
    [InlineData("ИЕК", "IEK", true)]
    [InlineData("ИЭК", "IEK", true)]
    [InlineData("IEK пром серия", "IEK", true)]

    // ABB
    [InlineData("ABB", "ABB", false)]
    [InlineData("АВВ", "ABB", true)]
    [InlineData("abb пром серия", "ABB", true)]

    // DEKraft
    [InlineData("DEKraft", "DEKraft", false)]
    [InlineData("dekraft", "DEKraft", true)]

    // КЭАЗ
    [InlineData("КЭАЗ", "КЭАЗ", false)]
    [InlineData("КЕАЗ", "КЭАЗ", true)]
    public void NormalizeManufacturerNameMapsApprovedAliases(
        string rawName,
        string expectedName,
        bool expectedWasChanged)
    {
        // Act
        var result =
            ManufacturerImportNormalizer.NormalizeManufacturerName(rawName);

        // Assert
        Assert.Equal(expectedName, result.NormalizedName);
        Assert.Equal(expectedWasChanged, result.WasChanged);
    }

    // Проверяет, что неизвестный производитель только очищается от внешних пробелов и не переименовывается.
    [Fact]
    public void NormalizeManufacturerNamePreservesUnknownTrimmedName()
    {
        // Arrange
        const string rawName = "  Schneider Electric  ";

        // Act
        var result =
            ManufacturerImportNormalizer.NormalizeManufacturerName(rawName);

        // Assert
        Assert.Equal("Schneider Electric", result.RawName);
        Assert.Equal("Schneider Electric", result.NormalizedName);
        Assert.False(result.WasChanged);
    }

    // Проверяет, что суффикс «пром серия» удаляется только у известных брендов из словаря.
    [Fact]
    public void NormalizeManufacturerNameDoesNotRemoveIndustrialSuffixFromUnknownBrand()
    {
        // Arrange
        const string rawName = "Unknown пром серия";

        // Act
        var result =
            ManufacturerImportNormalizer.NormalizeManufacturerName(rawName);

        // Assert
        Assert.Equal(rawName, result.RawName);
        Assert.Equal(rawName, result.NormalizedName);
        Assert.False(result.WasChanged);
    }
}
