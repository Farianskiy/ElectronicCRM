using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportBatch;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportBatch;

[ApiController]
[Authorize(Roles =
    "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class
    GetCatalogImportBatchController
    : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(
        typeof(GetCatalogImportBatchResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        GetCatalogImportBatchResponse>> Get(
            Guid batchId,
            [FromServices]
            GetCatalogImportBatchQueryHandler
                handler,
            CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogImportBatchQuery(
                batchId,
                currentUserId);

        var result =
            await handler
                .Handle(
                    query,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var value = result.Value;

        var response = new GetCatalogImportBatchResponse(
            value.BatchId,
            value.CreatedByUserId,
            value.ProductTypeId,
            value.OriginalFileName,
            value.FileSizeBytes,
            value.Status.ToString(),
            value.RowsCount,
            value.ValidRowsCount,
            value.ErrorRowsCount,
            value.CreatedAtUtc,
            value.UpdatedAtUtc,
            value.SubmittedAtUtc,
            value.ReviewedByUserId,
            value.ReviewedAtUtc,
            value.ChangesRequestedByUserId,
            value.ChangesRequestedAtUtc,
            value.ChangesRequestComment,
            value.RejectedByUserId,
            value.RejectedAtUtc,
            value.RejectionReason,
            value.AppliedByUserId,
            value.AppliedAtUtc,
            value.Version,
            value.CanEdit,
            value.CanSubmit,
            value.CanApply,
            value.CanRequestChanges,
            value.CanReject,
            value.CanDownloadFile,
            value.CanDelete);

        return Ok(response);
    }


}