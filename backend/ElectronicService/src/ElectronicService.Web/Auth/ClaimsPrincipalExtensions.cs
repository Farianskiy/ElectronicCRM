using System.Security.Claims;

namespace ElectronicService.Web.Auth;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal principal,
        out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(
            principal);

        var rawUserId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("user_id");

        return Guid.TryParse(
            rawUserId,
            out userId);
    }
}