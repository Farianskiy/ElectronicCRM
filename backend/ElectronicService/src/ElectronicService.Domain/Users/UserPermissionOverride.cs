using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Domain.Users;

public sealed class UserPermissionOverride
{
    private UserPermissionOverride(Guid userId, UserPermissionCode permissionCode, bool isAllowed)
    {
        UserId = userId;
        PermissionCode = permissionCode;
        IsAllowed = isAllowed;
    }

    private UserPermissionOverride()
    {
    }

    public Guid UserId { get; private set; }

    public UserPermissionCode PermissionCode { get; private set; }

    public bool IsAllowed { get; private set; }

    public static UserPermissionOverride Create(Guid userId, UserPermissionCode permissionCode, bool isAllowed)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (permissionCode == UserPermissionCode.None)
        {
            throw new ArgumentException("Permission code is required.", nameof(permissionCode));
        }

        return new UserPermissionOverride(userId, permissionCode, isAllowed);
    }
}