using System.Security.Claims;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Users.Enums;
using Microsoft.AspNetCore.Authentication;

namespace ElectronicService.Web.Auth;

public sealed class AdminRoleClaimsTransformation : IClaimsTransformation
{
    private static readonly string[] SystemDeveloperRoles = ["Administrator", "Technical", "Manager", "Regular"];

    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _overrideRepository;

    public AdminRoleClaimsTransformation(IUserRepository userRepository, IUserPermissionOverrideRepository overrideRepository)
    {
        _userRepository = userRepository;
        _overrideRepository = overrideRepository;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        if (principal.IsInRole("SystemDeveloper"))
        {
            foreach (var role in SystemDeveloperRoles.Where(role => !principal.IsInRole(role)))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        foreach (var claim in identity.FindAll(PermissionClaimTypes.Permission).ToArray())
        {
            identity.RemoveClaim(claim);
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return principal;
        }

        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);

        if (user is null || user.Status != UserStatus.Active)
        {
            return principal;
        }

        var permissions = UserPermissionCatalog.GetDefaults(user.Type).ToHashSet();
        var overrides = await _overrideRepository.GetByUserIdAsync(user.Id).ConfigureAwait(false);

        foreach (var permissionOverride in overrides)
        {
            if (permissionOverride.IsAllowed)
            {
                permissions.Add(permissionOverride.PermissionCode);
            }
            else
            {
                permissions.Remove(permissionOverride.PermissionCode);
            }
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(PermissionClaimTypes.Permission, permission.ToString()));
        }

        return principal;
    }
}