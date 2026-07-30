using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportReviewQueue;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportReviewQueue;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportReviewQueueController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
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
            return this.ToCurrentUserProblem();
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
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
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
}