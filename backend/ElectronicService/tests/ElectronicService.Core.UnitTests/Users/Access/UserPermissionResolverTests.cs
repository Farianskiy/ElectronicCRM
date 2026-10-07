using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.TestCommon;

namespace ElectronicService.Core.UnitTests.Users.Access;

public sealed class UserPermissionResolverTests
{
    [Fact]
    public void HasPermissionUsesRoleDefaultWithoutOverride()
    {
        var user = TestDataFactory.CreateRegularUser();

        Assert.True(UserPermissionResolver.HasPermission(
            user,
            UserPermissionCode.ProductsView));
        Assert.False(UserPermissionResolver.HasPermission(
            user,
            UserPermissionCode.CatalogImportsReview));
    }

    [Fact]
    public void HasPermissionAppliesExplicitGrant()
    {
        var user = TestDataFactory.CreateRegularUser();

        Assert.True(UserPermissionResolver.HasPermission(
            user,
            UserPermissionCode.CatalogImportsReview,
            overrideValue: true));
    }

    [Fact]
    public void HasPermissionAppliesExplicitDenial()
    {
        var user = TestDataFactory.CreateTechnicalUser();

        Assert.False(UserPermissionResolver.HasPermission(
            user,
            UserPermissionCode.CatalogImportsReview,
            overrideValue: false));
    }

    [Fact]
    public void HasPermissionRejectsBlockedUserEvenWithGrant()
    {
        var user = TestDataFactory.CreateRegularUser();
        var blockResult = user.Block();

        Assert.True(blockResult.IsSuccess);
        Assert.False(UserPermissionResolver.HasPermission(
            user,
            UserPermissionCode.CatalogImportsReview,
            overrideValue: true));
    }
}
