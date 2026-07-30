using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.AnalyzeCatalogImportBatch;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.AnalyzeCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class AnalyzeCatalogImportBatchController : ControllerBase
{
    private const string ProblemTitle = "Не удалось проанализировать Excel-файл.";

    [HttpPost("{batchId:guid}/analyze")]
    [ProducesResponseType(typeof(AnalyzeCatalogImportBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnalyzeCatalogImportBatchResponse>> Analyze(
        Guid batchId,
        [FromQuery] Guid? productTypeId,
        [FromServices] AnalyzeCatalogImportBatchCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var command = new AnalyzeCatalogImportBatchCommand(
            batchId,
            currentUserId,
            productTypeId);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
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
}