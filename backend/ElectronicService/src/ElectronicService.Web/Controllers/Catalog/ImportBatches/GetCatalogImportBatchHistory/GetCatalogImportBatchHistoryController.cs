using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportBatchHistory;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportBatchHistory;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportBatchHistoryController : ControllerBase
{
    [HttpGet("{batchId:guid}/history")]
    [ProducesResponseType(
        typeof(GetCatalogImportBatchHistoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCatalogImportBatchHistoryResponse>> Get(
        Guid batchId,
        [FromServices] GetCatalogImportBatchHistoryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Пользователь не определён.",
                detail:
                    "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var query =
            new GetCatalogImportBatchHistoryQuery(
                batchId,
                currentUserId);

        var result = await handler
            .Handle(
                query,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var items = result.Value.Items
            .Select(item =>
                new CatalogImportBatchHistoryItemResponse(
                    item.EventType,
                    item.OccurredAtUtc,
                    item.ActorUserId,
                    item.ActorDisplayName,
                    item.ActorEmail,
                    item.ActorUserType,
                    item.Comment))
            .ToArray();

        var response =
            new GetCatalogImportBatchHistoryResponse(
                result.Value.BatchId,
                items);

        return Ok(response);
    }

    private ObjectResult ToProblem(
        DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title =
                    "Не удалось получить историю пакета.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}