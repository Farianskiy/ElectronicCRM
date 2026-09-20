using ElectronicService.Contracts.Users;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Users.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers;

[ApiController]
[Route("api/users/me/access")]
[Authorize]
public sealed class CurrentUserAccessController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetUserAccessResponse>> GetCurrentUserAccess([FromServices] ICurrentUserProvider currentUserProvider, [FromServices] GetUserAccessQueryHandler handler, CancellationToken cancellationToken)
    {
        if (currentUserProvider.UserId is not Guid userId)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(new GetUserAccessQuery(userId), cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return NotFound();
        }

        var value = result.Value;
        var permissions = value.Permissions.Select(permission => new GetUserAccessResponseItem(permission.Code, permission.Group, permission.Label, permission.IsAllowed, permission.IsOverridden)).ToArray();

        return Ok(new GetUserAccessResponse(value.UserId, value.UserType, permissions));
    }
}