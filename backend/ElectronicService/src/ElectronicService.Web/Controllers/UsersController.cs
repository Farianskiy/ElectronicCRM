using CSharpFunctionalExtensions;
using ElectronicService.Contracts.Users;
using ElectronicService.Core.Users.BlockUser;
using ElectronicService.Core.Users.CreateRegularUser;
using ElectronicService.Core.Users.CreateTechnicalUser;
using ElectronicService.Core.Users.MakeUserRegular;
using ElectronicService.Core.Users.MakeUserTechnical;
using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
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