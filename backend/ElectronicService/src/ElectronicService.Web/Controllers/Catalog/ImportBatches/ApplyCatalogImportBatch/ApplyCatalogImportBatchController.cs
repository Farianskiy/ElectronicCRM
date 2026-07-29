using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.ApplyCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class ApplyCatalogImportBatchController : ControllerBase
{
    [HttpPost("{batchId:guid}/apply")]
    [ProducesResponseType(typeof(ApplyCatalogImportBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApplyCatalogImportBatchResponse>> Apply(
        Guid batchId,
        [FromServices] ApplyCatalogImportBatchCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var command = new ApplyCatalogImportBatchCommand(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var response = new ApplyCatalogImportBatchResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.AppliedByUserId,
            result.Value.AppliedAtUtc,
            result.Value.CreatedProductsCount,
            result.Value.Version);

        return Ok(response);
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.user.cannot_apply"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.cannot_be_applied_by_current_user"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.product_type.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.manufacturer.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.apply.characteristic_definition_not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.batch.invalid_status_transition"
                => StatusCodes.Status409Conflict,

            "catalog.import.apply.duplicate_article"
                => StatusCodes.Status409Conflict,

            "catalog.import.apply.article_already_exists"
                => StatusCodes.Status409Conflict,

            "catalog.import.apply.concurrency_conflict"
                => StatusCodes.Status409Conflict,

            "catalog.import.apply.database_failure"
                => StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось применить пакет импорта.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}