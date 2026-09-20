using ElectronicService.Core.Catalog.Characteristics.Normalization;

namespace ElectronicService.Core.UnitTests.Catalog.Characteristics.Normalization;

public sealed class CatalogPoleConfigurationNormalizerTests
{
    [Theory]
    [InlineData("1", "1P")]
    [InlineData("1p", "1P")]
    [InlineData("1п", "1P")]
    [InlineData("1р", "1P")]
    [InlineData(" 1 P ", "1P")]
    [InlineData("1P+N", "1P+N")]
    [InlineData("1p + n", "1P+N")]
    [InlineData("1п+н", "1P+N")]
    [InlineData("1р + н", "1P+N")]
    [InlineData("2", "2P")]
    [InlineData("2п", "2P")]
    [InlineData("3", "3P")]
    [InlineData("3P+N", "3P+N")]
    [InlineData("3п + н", "3P+N")]
    [InlineData("4", "4P")]
    [InlineData("4р", "4P")]
    public void TryNormalizeReturnsCanonicalValue(string rawValue, string expectedValue)
    {
        var result = CatalogPoleConfigurationNormalizer.TryNormalize(rawValue, out var normalizedValue);

        Assert.True(result);
        Assert.Equal(expectedValue, normalizedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0P")]
    [InlineData("2P+N")]
    [InlineData("5P")]
    [InlineData("двухполюсный")]
    public void TryNormalizeRejectsUnsupportedValue(string? rawValue)
    {
        var result = CatalogPoleConfigurationNormalizer.TryNormalize(rawValue, out var normalizedValue);

        Assert.False(result);
        Assert.Equal(string.Empty, normalizedValue);
    }

    [Fact]
    public void ExpandForSearchIncludesOnePoleWithNeutralForTwoPoleRequest()
    {
        var values = CatalogPoleConfigurationNormalizer.ExpandForSearch("2п");

        Assert.Equal(["2P", "1P+N"], values);
    }

    [Fact]
    public void ExpandForSearchIncludesThreePoleWithNeutralForFourPoleRequest()
    {
        var values = CatalogPoleConfigurationNormalizer.ExpandForSearch("4P");

        Assert.Equal(["4P", "3P+N"], values);
    }

    [Theory]
    [InlineData("1P+N")]
    [InlineData("3P+N")]
    public void ExpandForSearchKeepsNeutralConfigurationExact(string rawValue)
    {
        var values = CatalogPoleConfigurationNormalizer.ExpandForSearch(rawValue);

        Assert.Equal([rawValue], values);
    }

    [Fact]
    public void ExpandForSearchReturnsEmptyCollectionForUnsupportedValue()
    {
        var values = CatalogPoleConfigurationNormalizer.ExpandForSearch("5P");

        Assert.Empty(values);
    }
}