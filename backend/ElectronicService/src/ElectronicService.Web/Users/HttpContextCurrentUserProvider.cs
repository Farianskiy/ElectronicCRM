using System.Security.Claims;
using ElectronicService.Core.Abstractions;

namespace ElectronicService.Web.Users;

public sealed class HttpContextCurrentUserProvider : ICurrentUserProvider
{
    private const string UserIdHeaderName = "X-User-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserProvider(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                return null;
            }

            var claimUserId =
                httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub")
                ?? httpContext.User.FindFirstValue("user_id");

            if (Guid.TryParse(claimUserId, out var userIdFromClaim))
            {
                return userIdFromClaim;
            }

            if (!httpContext.Request.Headers.TryGetValue(
                    UserIdHeaderName,
                    out var userIdHeader))
            {
                return null;
            }

            return Guid.TryParse(userIdHeader.ToString(), out var userIdFromHeader)
                ? userIdFromHeader
                : null;
        }
    }
}