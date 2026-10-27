using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.PreviewExampleFill;

[ApiController]
[PermissionAuthorize(UserPermissionCode.CatalogImportsCreate)]
[Route("api/catalog/import-batches")]
public sealed class PreviewCatalogImportExampleFillController : ControllerBase
{
    private const string ProblemTitle = "Не удалось подготовить заполнение по примерам.";

    [HttpPost("{batchId:guid}/example-fill/preview")]
    [ProducesResponseType(typeof(PreviewCatalogImportExampleFillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreviewCatalogImportExampleFillResponse>> Preview(
        Guid batchId,
        [FromBody] PreviewCatalogImportExampleFillRequest request,
        [FromServices] PreviewCatalogImportExampleFillQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var result = await handler.Handle(
            new PreviewCatalogImportExampleFillQuery(
                batchId,
                currentUserId,
                request.ExpectedVersion,
                request.ExampleRowIds,
                request.TargetRowIds),
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        return Ok(new PreviewCatalogImportExampleFillResponse(
            result.Value.Version,
            result.Value.ExamplesCount,
            result.Value.TargetRowsCount,
            result.Value.ReadyRowsCount,
            result.Value.ReviewRowsCount,
            result.Value.ConflictRowsCount,
            result.Value.UnmatchedRowsCount,
            result.Value.Rows.Select(row => new PreviewCatalogImportExampleFillRowResponse(
                row.RowId,
                row.RowNumber,
                row.ProductName,
                row.State,
                row.Proposals.Select(proposal => new PreviewCatalogImportExampleFillProposalResponse(
                    proposal.CharacteristicDefinitionId,
                    proposal.CharacteristicName,
                    proposal.ProposedValue,
                    proposal.CurrentValue,
                    proposal.MatchedRawValue,
                    proposal.Confidence,
                    proposal.SourceExampleIds,
                    proposal.SourceRowIds)).ToArray(),
                row.Conflicts.Select(conflict => new PreviewCatalogImportExampleFillConflictResponse(
                    conflict.CharacteristicDefinitionId,
                    conflict.CharacteristicName,
                    conflict.Reason,
                    conflict.CurrentValue,
                    conflict.ProposedValues,
                    conflict.SourceExampleIds)).ToArray())).ToArray()));
    }
}
