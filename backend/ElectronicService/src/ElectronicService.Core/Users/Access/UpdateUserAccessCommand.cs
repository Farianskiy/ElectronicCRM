using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Users.Access;

public sealed record UpdateUserAccessCommand(
    Guid UserId,
    UserType UserType,
    IReadOnlyCollection<UserPermissionCode> AllowedPermissions);