using ElectronicService.Contracts.Auth;
using ElectronicService.Core.Auth.Login;
using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginCommandHandler _handler;

    public AuthController(LoginCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new LoginCommand(
            request.Email,
            request.Password);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new LoginResponse(
            result.Value.AccessToken,
            result.Value.UserId,
            result.Value.UserType,
            result.Value.DisplayName));
    }
}