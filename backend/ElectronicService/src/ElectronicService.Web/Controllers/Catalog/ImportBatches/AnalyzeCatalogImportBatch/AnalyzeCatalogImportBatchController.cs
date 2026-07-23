using ElectronicService.Contracts.Catalog
    .ImportBatches;
using ElectronicService.Core.Catalog
    .ImportBatches.AnalyzeCatalogImportBatch;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ImportBatches.AnalyzeCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class
    AnalyzeCatalogImportBatchController
    : ControllerBase
{
    [HttpPost("{batchId:guid}/analyze")]
    [ProducesResponseType(
        typeof(AnalyzeCatalogImportBatchResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<
        AnalyzeCatalogImportBatchResponse>>
        Analyze(
            Guid batchId,
            [FromQuery] Guid? productTypeId,
            [FromServices]
            AnalyzeCatalogImportBatchCommandHandler
                handler,
            CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(
                out var currentUserId))
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Пользователь не определён.",
                detail:
                    "В JWT отсутствует корректный " +
                    "идентификатор пользователя.");
        }

        var command =
            new AnalyzeCatalogImportBatchCommand(
                batchId,
                currentUserId,
                productTypeId);

        var result =
            await handler
                .Handle(
                    command,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Ok(
            new AnalyzeCatalogImportBatchResponse(
                result.Value.BatchId,
                result.Value.Status.ToString(),
                result.Value.ProductTypeId,
                result.Value.ColumnsCount,
                result.Value.UnmappedColumnsCount,
                result.Value.UnconfirmedColumnsCount,
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount));
    }

    private ObjectResult ToProblem(
        DomainError error)
    {
        var statusCode =
            error.Code switch
            {
                "catalog.import.batch.not_found"
                    => StatusCodes.Status404NotFound,

                "catalog.import.product_type.not_found"
                    => StatusCodes.Status404NotFound,

                "catalog.import.batch.access_denied"
                    => StatusCodes.Status403Forbidden,

                "catalog.import.user.cannot_create"
                    => StatusCodes.Status403Forbidden,

                "catalog.import.current_user.not_found"
                    => StatusCodes.Status401Unauthorized,

                "catalog.import.batch.cannot_be_analyzed"
                    => StatusCodes.Status409Conflict,

                "catalog.import.batch.invalid_status_transition"
                    => StatusCodes.Status409Conflict,

                _ => StatusCodes.Status400BadRequest
            };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title =
                    "Не удалось проанализировать " +
                    "Excel-файл.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}