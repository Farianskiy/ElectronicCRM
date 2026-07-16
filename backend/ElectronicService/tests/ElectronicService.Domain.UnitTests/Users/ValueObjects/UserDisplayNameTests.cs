using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Users.ValueObjects;

public sealed class UserDisplayNameTests
{
    // Проверяет очистку корректного отображаемого имени пользователя.
    [Fact]
    public void CreateTrimsValidDisplayName()
    {
        var result = UserDisplayName.Create("  Fer  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fer", result.Value.Value);
    }

    // Проверяет обязательность отображаемого имени.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateReturnsRequiredErrorForBlankName(string name)
    {
        var result = UserDisplayName.Create(name);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    // Проверяет минимальную длину отображаемого имени.
    [Fact]
    public void CreateReturnsTooShortErrorForOneCharacter()
    {
        var result = UserDisplayName.Create("F");

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_short", result.Error.Code);
    }

    // Проверяет максимальную длину отображаемого имени.
    [Fact]
    public void CreateReturnsTooLongErrorWhenNameExceedsLimit()
    {
        var result = UserDisplayName.Create(new string('F', 101));

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_long", result.Error.Code);
    }
}
