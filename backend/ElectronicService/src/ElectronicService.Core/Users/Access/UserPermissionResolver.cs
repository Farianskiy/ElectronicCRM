using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Users.Access;

public static class UserPermissionResolver
{
    public static bool HasPermission(
        User user,
        UserPermissionCode permission,
        bool? overrideValue = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsActive || permission == UserPermissionCode.None)
        {
            return false;
        }

        return overrideValue
            ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(permission);
    }
}
