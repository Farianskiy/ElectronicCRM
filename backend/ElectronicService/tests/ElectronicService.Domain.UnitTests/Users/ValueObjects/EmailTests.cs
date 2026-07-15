using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Users.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void CreateTrimsAndNormalizesValidEmail()
    {
        var result = Email.Create("  fer@example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("FER@EXAMPLE.COM", result.Value.Value);
        Assert.Equal("FER@EXAMPLE.COM", result.Value.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateReturnsRequiredErrorForBlankEmail(string email)
    {
        var result = Email.Create(email);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    [Theory]
    [InlineData("ferexample.com")]
    [InlineData("@example.com")]
    [InlineData("fer@")]
    [InlineData("fer@@example.com")]
    [InlineData("fer@example")]
    public void CreateReturnsInvalidErrorForMalformedEmail(string email)
    {
        var result = Email.Create(email);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    [Fact]
    public void CreateReturnsTooLongErrorWhenEmailExceedsLimit()
    {
        var email = $"{new string('a', 310)}@example.com";

        var result = Email.Create(email);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_long", result.Error.Code);
    }

    [Fact]
    public void EqualityIgnoresCaseBecauseEmailIsNormalized()
    {
        var first = Email.Create("fer@example.com").Value;
        var second = Email.Create("FER@EXAMPLE.COM").Value;

        Assert.Equal(first, second);
    }
}
