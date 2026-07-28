using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportReviewQueue;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportReviewQueue;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportReviewQueueController : ControllerBase
{
    [HttpGet("review-queue")]
    [ProducesResponseType(typeof(GetCatalogImportReviewQueueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetCatalogImportReviewQueueResponse>> Get(
        [FromServices] GetCatalogImportReviewQueueQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] CatalogImportBatchStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var query = new GetCatalogImportReviewQueueQuery(
            currentUserId,
            status,
            page,
            pageSize);

        var result = await handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var items = result.Value.Items
            .Select(item =>
                new CatalogImportReviewQueueItemResponse(
                    item.BatchId,
                    item.CreatedByUserId,
                    item.CreatedByDisplayName,
                    item.CreatedByEmail,
                    item.CreatedByUserType,
                    item.ProductTypeId,
                    item.OriginalFileName,
                    item.Status.ToString(),
                    item.RowsCount,
                    item.ValidRowsCount,
                    item.ErrorRowsCount,
                    item.CreatedAtUtc,
                    item.SubmittedAtUtc,
                    item.ReviewedByUserId,
                    item.ReviewedAtUtc,
                    item.Version))
            .ToArray();

        var response = new GetCatalogImportReviewQueueResponse(
            items,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount,
            result.Value.TotalPages);

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

            "catalog.import.rows.invalid_pagination"
                => StatusCodes.Status400BadRequest,

            "catalog.import.review_queue.invalid_status"
                => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось получить очередь проверки импортов.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}