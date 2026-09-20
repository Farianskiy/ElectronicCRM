using ElectronicService.Domain.Users.Enums;
using Microsoft.AspNetCore.Authorization;

namespace ElectronicService.Web.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class PermissionAnyAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAnyAuthorizeAttribute(params UserPermissionCode[] permissions)
    {
        if (permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        Permissions = permissions;
        Policy = PermissionPolicy.ForAny(permissions);
    }

    public IReadOnlyList<UserPermissionCode> Permissions { get; }
}