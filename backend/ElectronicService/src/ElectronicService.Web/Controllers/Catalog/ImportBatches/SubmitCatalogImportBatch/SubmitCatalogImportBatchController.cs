using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.SubmitCatalogImportBatch;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.SubmitCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Regular,Manager")]
[Route("api/catalog/import-batches")]
public sealed class SubmitCatalogImportBatchController : ControllerBase
{
    [HttpPost("{batchId:guid}/submit")]
    [ProducesResponseType(typeof(SubmitCatalogImportBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitCatalogImportBatchResponse>> Submit(
        Guid batchId,
        [FromServices] SubmitCatalogImportBatchCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var command = new SubmitCatalogImportBatchCommand(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var response = new SubmitCatalogImportBatchResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.SubmittedAtUtc,
            result.Value.Version);

        return Ok(response);
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.user.cannot_submit"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.batch.invalid_status_transition"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.product_type_required"
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось отправить пакет импорта на проверку.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}