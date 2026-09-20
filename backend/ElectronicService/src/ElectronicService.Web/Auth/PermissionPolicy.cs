using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Web.Auth;

public static class PermissionPolicy
{
    public static string For(UserPermissionCode permission) => $"Permission:{permission}";

    public static string ForAny(params UserPermissionCode[] permissions) =>
        $"PermissionAny:{string.Join(',', permissions)}";
}