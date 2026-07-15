using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Catalog.ValueObjects;

public sealed class ProductArticleTests
{
    [Fact]
    public void CreateReturnsTrimmedArticleForValidValue()
    {
        // Arrange
        const string article = "  NB1-63-1P-C10  ";

        // Act
        var result = ProductArticle.Create(article);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NB1-63-1P-C10", result.Value.Value);
        Assert.Equal("NB1-63-1P-C10", result.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateReturnsRequiredErrorForBlankValue(string article)
    {
        var result = ProductArticle.Create(article);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    [Fact]
    public void CreateReturnsRequiredErrorForNullValue()
    {
        var result = ProductArticle.Create(null!);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    [Fact]
    public void CreateReturnsTooLongErrorWhenArticleExceedsLimit()
    {
        var article = new string('A', 101);

        var result = ProductArticle.Create(article);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_long", result.Error.Code);
    }

    [Fact]
    public void EqualityIgnoresLetterCase()
    {
        var first = ProductArticle.Create("nb1-63").Value;
        var second = ProductArticle.Create("NB1-63").Value;

        Assert.Equal(first, second);
    }
}
