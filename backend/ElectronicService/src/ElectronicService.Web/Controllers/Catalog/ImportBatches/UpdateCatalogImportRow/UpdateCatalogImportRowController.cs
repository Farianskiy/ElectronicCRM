using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportRow;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.UpdateCatalogImportRow;

[ApiController]
[PermissionAnyAuthorize(UserPermissionCode.CatalogImportsCreate, UserPermissionCode.CatalogImportsReview)]
[Route("api/catalog/import-batches")]
public sealed class UpdateCatalogImportRowController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpPatch("{batchId:guid}/rows/{rowId:guid}")]
    [ProducesResponseType(typeof(UpdateCatalogImportRowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateCatalogImportRowResponse>> Update(
        Guid batchId,
        Guid rowId,
        [FromBody] UpdateCatalogImportRowRequest request,
        [FromServices] UpdateCatalogImportRowCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        if (request.ConfirmedSpans is not null && request.ConfirmedSpans.Any(item => item.Value is null))
        {
            return BadRequest(new ProblemDetails { Title = ProblemTitle, Detail = "Разметка характеристики не может быть null.", Status = StatusCodes.Status400BadRequest });
        }

        var confirmedSpans = request.ConfirmedSpans?.ToDictionary(item => item.Key, item => new CatalogImportConfirmedSpan(item.Value.ProductName, item.Value.Start, item.Value.Length));

        var command = new UpdateCatalogImportRowCommand(
            batchId,
            rowId,
            currentUserId,
            request.Name,
            request.Article,
            request.ProductTypeId,
            request.ManufacturerId,
            request.Price,
            request.StockQuantity,
            request.Characteristics ?? new Dictionary<string, string>(StringComparer.Ordinal),
            confirmedSpans);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var value = result.Value;

        var data = new CatalogImportNormalizedRowResponse(
            value.Data.Name,
            value.Data.Article,
            value.Data.Manufacturer,
            value.Data.ManufacturerId,
            value.Data.Price,
            value.Data.StockQuantity,
            value.Data.Characteristics,
            value.Data.ProductTypeId,
            value.Data.ProductTypeResolutionSource,
            value.Data.ProductTypeResolutionConfidence);

        var issues = value.Issues
            .Select(issue =>
                new CatalogImportRowIssueResponse(
                    issue.Code,
                    issue.Message,
                    issue.Field,
                    issue.SourceColumnNumber))
            .ToArray();

        var warnings = value.Warnings
            .Select(warning =>
                new CatalogImportRowIssueResponse(
                    warning.Code,
                    warning.Message,
                    warning.Field,
                    warning.SourceColumnNumber))
            .ToArray();

        var response = new UpdateCatalogImportRowResponse(
            value.RowId,
            value.RowNumber,
            value.RowStatus.ToString(),
            data,
            issues,
            warnings,
            value.BatchStatus.ToString(),
            value.RowsCount,
            value.ValidRowsCount,
            value.ErrorRowsCount,
            value.Version);

        return Ok(response);
    }
}
