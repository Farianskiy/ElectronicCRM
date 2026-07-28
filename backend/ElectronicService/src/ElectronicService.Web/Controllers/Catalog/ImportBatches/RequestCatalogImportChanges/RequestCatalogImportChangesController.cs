using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.RequestCatalogImportChanges;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.RequestCatalogImportChanges;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class RequestCatalogImportChangesController : ControllerBase
{
    [HttpPost("{batchId:guid}/review/request-changes")]
    [ProducesResponseType(typeof(RequestCatalogImportChangesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestCatalogImportChangesResponse>> RequestChanges(
        Guid batchId,
        [FromBody] RequestCatalogImportChangesRequest request,
        [FromServices] RequestCatalogImportChangesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var command = new RequestCatalogImportChangesCommand(
            batchId,
            currentUserId,
            request.Comment);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var response = new RequestCatalogImportChangesResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.ChangesRequestedByUserId,
            result.Value.ChangesRequestedAtUtc,
            result.Value.Comment,
            result.Value.Version);

        return Ok(response);
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.user.cannot_review"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.batch.invalid_status_transition"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.review_assigned_to_another_user"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.concurrency_conflict"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.changes_request_comment_required"
                => StatusCodes.Status400BadRequest,

            "catalog.import.batch.changes_request_comment_too_long"
                => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось вернуть пакет импорта на исправление.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}