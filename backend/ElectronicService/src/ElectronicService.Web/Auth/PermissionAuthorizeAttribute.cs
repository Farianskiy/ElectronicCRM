using ElectronicService.Domain.Users.Enums;
using Microsoft.AspNetCore.Authorization;

namespace ElectronicService.Web.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(UserPermissionCode permission)
    {
        Permission = permission;
        Policy = PermissionPolicy.For(permission);
    }

    public UserPermissionCode Permission { get; }
}