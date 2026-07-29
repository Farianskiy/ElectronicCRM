using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetMyCatalogImportBatches;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetMyCatalogImportBatches;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetMyCatalogImportBatchesController : ControllerBase
{
    [HttpGet("my")]
    [ProducesResponseType(typeof(GetMyCatalogImportBatchesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetMyCatalogImportBatchesResponse>> Get(
        [FromServices] GetMyCatalogImportBatchesQueryHandler handler,
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

        var query = new GetMyCatalogImportBatchesQuery(
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
            .Select(item => new MyCatalogImportBatchItemResponse(
                item.BatchId,
                item.ProductTypeId,
                item.OriginalFileName,
                item.FileSizeBytes,
                item.Status.ToString(),
                item.RowsCount,
                item.ValidRowsCount,
                item.ErrorRowsCount,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.LastActivityAtUtc,
                item.SubmittedAtUtc,
                item.ChangesRequestedAtUtc,
                item.ChangesRequestComment,
                item.RejectedAtUtc,
                item.RejectionReason,
                item.AppliedAtUtc,
                item.Version,
                item.CanEdit,
                item.CanSubmit,
                item.CanApply,
                item.CanDelete))
            .ToArray();

        var response = new GetMyCatalogImportBatchesResponse(
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

            "catalog.import.user.cannot_view_own"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batches.invalid_pagination"
                => StatusCodes.Status400BadRequest,

            "catalog.import.batches.invalid_status"
                => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось получить список импортов.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}