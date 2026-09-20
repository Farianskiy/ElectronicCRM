using CSharpFunctionalExtensions;
using ElectronicService.Contracts.Users;
using ElectronicService.Core.Users.BlockUser;
using ElectronicService.Core.Users.Access;
using ElectronicService.Core.Users.CreateAdministratorUser;
using ElectronicService.Core.Users.CreateRegularUser;
using ElectronicService.Core.Users.CreateTechnicalUser;
using ElectronicService.Core.Users.GetUsers;
using ElectronicService.Core.Users.MakeUserRegular;
using ElectronicService.Core.Users.MakeUserTechnical;
using ElectronicService.Core.Users.CreateManagerUser;
using ElectronicService.Core.Users.MakeUserManager;
using Microsoft.AspNetCore.Authorization;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers;

[ApiController]
[Route("api/users")]
[PermissionAuthorize(UserPermissionCode.UsersManage)]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetUsersResponse>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserType? type,
        [FromQuery] UserStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] GetUsersQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetUsersQuery(search, type, status, User.IsInRole("SystemDeveloper"), page, pageSize), cancellationToken).ConfigureAwait(false);
        var items = result.Items.Select(user => new GetUsersResponseItem(user.Id, user.DisplayName, user.Email, user.UserType, user.Status, user.CreatedAtUtc)).ToArray();

        return Ok(new GetUsersResponse(items, result.TotalCount, result.Page, result.PageSize));
    }

    [HttpGet("{id:guid}/access")]
    public async Task<ActionResult<GetUserAccessResponse>> GetUserAccess(Guid id, [FromServices] GetUserAccessQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetUserAccessQuery(id), cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var value = result.Value;
        var permissions = value.Permissions.Select(permission => new GetUserAccessResponseItem(permission.Code, permission.Group, permission.Label, permission.IsAllowed, permission.IsOverridden)).ToArray();

        return Ok(new GetUserAccessResponse(value.UserId, value.UserType, permissions));
    }

    [HttpPut("{id:guid}/access")]
    public async Task<IActionResult> UpdateUserAccess(Guid id, [FromBody] UpdateUserAccessRequest request, [FromServices] UpdateUserAccessCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserType>(request.UserType, true, out var userType) || userType is UserType.None or UserType.SystemDeveloper)
        {
            return BadRequest(new ProblemDetails { Title = "Request failed.", Detail = "Некорректная роль пользователя." });
        }

        var permissions = new List<UserPermissionCode>();

        foreach (var permission in request.AllowedPermissions)
        {
            if (!Enum.TryParse<UserPermissionCode>(permission, true, out var code) || code == UserPermissionCode.None)
            {
                return BadRequest(new ProblemDetails { Title = "Request failed.", Detail = $"Некорректное разрешение '{permission}'." });
            }

            permissions.Add(code);
        }

        var result = await handler.Handle(new UpdateUserAccessCommand(id, userType, permissions.Distinct().ToArray()), cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return NoContent();
    }

    // СОздает обычного пользователя
    [HttpPost("regular")]
    public async Task<IActionResult> CreateRegularUser(
        [FromBody] CreateRegularUserRequest request,
        [FromServices] CreateRegularUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateRegularUserCommand(
            request.DisplayName,
            request.Email,
            request.Password);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Created(
            new Uri($"/api/users/{result.Value}", UriKind.Relative),
            new UserResponse(result.Value));
    }

    // Создаёт технического пользователя
    [HttpPost("technical")]
    public async Task<IActionResult> CreateTechnicalUser(
        [FromBody] CreateTechnicalUserRequest request,
        [FromServices] CreateTechnicalUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateTechnicalUserCommand(
            request.DisplayName,
            request.Email,
            request.Password);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Created(
            new Uri($"/api/users/{result.Value}", UriKind.Relative),
            new UserResponse(result.Value));
    }

    [HttpPost("manager")]
    public async Task<IActionResult> CreateManagerUser(
        [FromBody] CreateManagerUserRequest request,
        [FromServices]
        CreateManagerUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command =
            new CreateManagerUserCommand(
                request.DisplayName,
                request.Email,
                request.Password);

        var result = await handler
            .Handle(
                command,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Created(
            new Uri(
                $"/api/users/{result.Value}",
                UriKind.Relative),
            new UserResponse(result.Value));
    }

    [HttpPost("administrator")]
    [Authorize(Roles = "SystemDeveloper")]
    public async Task<IActionResult> CreateAdministratorUser([FromBody] CreateAdministratorUserRequest request, [FromServices] CreateAdministratorUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateAdministratorUserCommand(request.DisplayName, request.Email, request.Password), cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Created(new Uri($"/api/users/{result.Value}", UriKind.Relative), new UserResponse(result.Value));
    }

    // Делает существующего пользователя техническим
    [HttpPost("{id:guid}/make-technical")]
    public async Task<IActionResult> MakeUserTechnical(
        Guid id,
        [FromBody] MakeUserTechnicalRequest request,
        [FromServices] MakeUserTechnicalCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new MakeUserTechnicalCommand(
            id,
            request.Email);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return NoContent();
    }

    // Делает технического пользователя обычным
    [HttpPost("{id:guid}/make-regular")]
    public async Task<IActionResult> MakeUserRegular(
        Guid id,
        [FromServices] MakeUserRegularCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new MakeUserRegularCommand(id);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/make-manager")]
    public async Task<IActionResult> MakeUserManager(
        Guid id,
        [FromBody]
        MakeUserManagerRequest request,
        [FromServices]
        MakeUserManagerCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command =
            new MakeUserManagerCommand(
                id,
                request.Email);

        var result = await handler
            .Handle(
                command,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return NoContent();
    }

    // Блокирует пользователя
    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> BlockUser(
        Guid id,
        [FromServices] BlockUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new BlockUserCommand(id);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return NoContent();
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = GetStatusCode(error);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "Request failed.",
            Detail = error.Message,
            Type = error.Code
        };

        return StatusCode(statusCode, problemDetails);
    }

    private static int GetStatusCode(DomainError error)
    {
        return error.Code switch
        {
            "user.not_found" => StatusCodes.Status404NotFound,
            "user.protected_system_developer" => StatusCodes.Status404NotFound,
            "user.administrator_role_requires_system_developer" => StatusCodes.Status403Forbidden,
            "user.already_manager" => StatusCodes.Status409Conflict,
            "user.email_already_taken" => StatusCodes.Status409Conflict,
            "user.already_technical" => StatusCodes.Status409Conflict,
            "user.already_regular" => StatusCodes.Status409Conflict,
            "user.already_blocked" => StatusCodes.Status409Conflict,
            "user.already_active" => StatusCodes.Status409Conflict,
            "user.blocked_user_cannot_be_changed" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
