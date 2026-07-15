using ElectronicService.TestCommon;

namespace ElectronicService.Domain.UnitTests.Users;

public sealed class UserTests
{
    [Fact]
    public void CreateRegularBuildsActiveRegularUser()
    {
        var result = ElectronicService.Domain.Users.User.CreateRegular(
            "  Fer  ",
            "  fer@example.com  ",
            "  password-hash  ");

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("Fer", result.Value.DisplayName.Value);
        Assert.Equal("FER@EXAMPLE.COM", result.Value.Email?.Value);
        Assert.Equal("password-hash", result.Value.PasswordHash);
        Assert.True(result.Value.IsRegular);
        Assert.False(result.Value.IsTechnical);
        Assert.True(result.Value.IsActive);
        Assert.False(result.Value.IsBlocked);
        Assert.True(result.Value.CanUseAssistant());
        Assert.True(result.Value.CanViewProducts());
        Assert.True(result.Value.CanFindProductAlternatives());
        Assert.False(result.Value.CanUpdateProductPrice());
        Assert.False(result.Value.CanUpdateStockBalance());
    }

    [Fact]
    public void CreateRegularAllowsMissingEmailAndPassword()
    {
        var result = ElectronicService.Domain.Users.User.CreateRegular(
            "Обычный пользователь",
            null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.PasswordHash);
        Assert.False(result.Value.HasPassword);
    }

    [Fact]
    public void CreateTechnicalBuildsUserWithTechnicalPermissions()
    {
        var user = TestDataFactory.CreateTechnicalUser();

        Assert.True(user.IsTechnical);
        Assert.True(user.IsActive);
        Assert.True(user.CanUseAssistant());
        Assert.True(user.CanViewProducts());
        Assert.True(user.CanFindProductAlternatives());
        Assert.True(user.CanUpdateProductPrice());
        Assert.True(user.CanUpdateStockBalance());
        Assert.True(user.CanApproveProductCorrections());
        Assert.True(user.CanManageProductSynonyms());
    }

    [Fact]
    public void CreateTechnicalRejectsInvalidEmail()
    {
        var result = ElectronicService.Domain.Users.User.CreateTechnical(
            "Технический пользователь",
            "invalid-email");

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    [Fact]
    public void MakeTechnicalChangesRoleAndEmail()
    {
        var user = TestDataFactory.CreateRegularUser(email: null);

        var result = user.MakeTechnical("tech@example.com");

        Assert.True(result.IsSuccess);
        Assert.True(user.IsTechnical);
        Assert.False(user.IsRegular);
        Assert.Equal("TECH@EXAMPLE.COM", user.Email?.Value);
        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Fact]
    public void MakeTechnicalRejectsAlreadyTechnicalUser()
    {
        var user = TestDataFactory.CreateTechnicalUser();

        var result = user.MakeTechnical("other@example.com");

        Assert.True(result.IsFailure);
        Assert.Equal("user.already_technical", result.Error.Code);
    }

    [Fact]
    public void MakeRegularChangesTechnicalUserRole()
    {
        var user = TestDataFactory.CreateTechnicalUser();

        var result = user.MakeRegular();

        Assert.True(result.IsSuccess);
        Assert.True(user.IsRegular);
        Assert.False(user.IsTechnical);
        Assert.False(user.CanUpdateProductPrice());
    }

    [Fact]
    public void BlockDisablesAllBusinessPermissions()
    {
        var user = TestDataFactory.CreateTechnicalUser();

        var result = user.Block();

        Assert.True(result.IsSuccess);
        Assert.True(user.IsBlocked);
        Assert.False(user.IsActive);
        Assert.False(user.CanUseAssistant());
        Assert.False(user.CanViewProducts());
        Assert.False(user.CanFindProductAlternatives());
        Assert.False(user.CanUpdateProductPrice());
        Assert.False(user.CanUpdateStockBalance());
        Assert.False(user.CanApproveProductCorrections());
        Assert.False(user.CanManageProductSynonyms());
    }

    [Fact]
    public void BlockRejectsAlreadyBlockedUser()
    {
        var user = TestDataFactory.CreateRegularUser();

        user.Block();

        var result = user.Block();

        Assert.True(result.IsFailure);
        Assert.Equal("user.already_blocked", result.Error.Code);
    }

    [Fact]
    public void BlockedUserCannotBeChanged()
    {
        var user = TestDataFactory.CreateTechnicalUser();
        var oldName = user.DisplayName;
        var oldEmail = user.Email;
        var oldPasswordHash = user.PasswordHash;

        user.Block();

        var nameResult = user.ChangeDisplayName("Новое имя");
        var emailResult = user.ChangeEmail("new@example.com");
        var roleResult = user.MakeRegular();
        var passwordResult = user.ChangePasswordHash("new-hash");

        Assert.True(nameResult.IsFailure);
        Assert.True(emailResult.IsFailure);
        Assert.True(roleResult.IsFailure);
        Assert.True(passwordResult.IsFailure);

        Assert.Equal(
            "user.blocked_user_cannot_be_changed",
            nameResult.Error.Code);
        Assert.Same(oldName, user.DisplayName);
        Assert.Same(oldEmail, user.Email);
        Assert.Equal(oldPasswordHash, user.PasswordHash);
        Assert.True(user.IsTechnical);
    }

    [Fact]
    public void ActivateRestoresActiveState()
    {
        var user = TestDataFactory.CreateRegularUser();

        user.Block();

        var result = user.Activate();

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.False(user.IsBlocked);
        Assert.True(user.CanUseAssistant());
    }

    [Fact]
    public void ChangeEmailNormalizesNewEmail()
    {
        var user = TestDataFactory.CreateRegularUser();

        var result = user.ChangeEmail("  new@example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("NEW@EXAMPLE.COM", user.Email?.Value);
    }

    [Fact]
    public void ChangePasswordHashTrimsNewHash()
    {
        var user = TestDataFactory.CreateRegularUser(passwordHash: null);

        var result = user.ChangePasswordHash("  new-password-hash  ");

        Assert.True(result.IsSuccess);
        Assert.True(user.HasPassword);
        Assert.Equal("new-password-hash", user.PasswordHash);
    }

    [Fact]
    public void ChangePasswordHashRejectsBlankValue()
    {
        var user = TestDataFactory.CreateRegularUser();
        var oldPasswordHash = user.PasswordHash;

        var result = user.ChangePasswordHash(" ");

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
        Assert.Equal(oldPasswordHash, user.PasswordHash);
    }
}
