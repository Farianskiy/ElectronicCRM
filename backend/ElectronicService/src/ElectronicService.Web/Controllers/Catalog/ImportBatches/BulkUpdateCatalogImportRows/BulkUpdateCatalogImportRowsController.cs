using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.BulkUpdateCatalogImportRows;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.BulkUpdateCatalogImportRows;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class BulkUpdateCatalogImportRowsController : ControllerBase
{
    [HttpPut("{batchId:guid}/rows/bulk")]
    [ProducesResponseType(typeof(BulkUpdateCatalogImportRowsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkUpdateCatalogImportRowsResponse>> Update(Guid batchId, [FromBody] BulkUpdateCatalogImportRowsRequest request, [FromServices] BulkUpdateCatalogImportRowsCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var requestRows = request.Rows ?? Array.Empty<BulkUpdateCatalogImportRowRequest>();

        var commandRows = requestRows
            .Select(row =>
                new BulkUpdateCatalogImportRowItem(
                    row.RowId,
                    row.Name,
                    row.Article,
                    row.ManufacturerId,
                    row.Price,
                    row.StockQuantity,
                    row.Characteristics ?? new Dictionary<string, string>(StringComparer.Ordinal)))
            .ToArray();

        var command = new BulkUpdateCatalogImportRowsCommand(
            batchId,
            currentUserId,
            request.ExpectedVersion,
            commandRows);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var value = result.Value;

        var responseRows = value.Rows
            .Select(row =>
            {
                var data = new CatalogImportNormalizedRowResponse(
                    row.Data.Name,
                    row.Data.Article,
                    row.Data.Manufacturer,
                    row.Data.ManufacturerId,
                    row.Data.Price,
                    row.Data.StockQuantity,
                    row.Data.Characteristics);

                var issues = row.Issues
                    .Select(issue =>
                        new CatalogImportRowIssueResponse(
                            issue.Code,
                            issue.Message,
                            issue.Field,
                            issue.SourceColumnNumber))
                    .ToArray();

                var warnings = row.Warnings
                    .Select(warning =>
                        new CatalogImportRowIssueResponse(
                            warning.Code,
                            warning.Message,
                            warning.Field,
                            warning.SourceColumnNumber))
                    .ToArray();

                return new BulkUpdatedCatalogImportRowResponse(
                    row.RowId,
                    row.RowNumber,
                    row.RowStatus.ToString(),
                    data,
                    issues,
                    warnings);
            })
            .ToArray();

        var response = new BulkUpdateCatalogImportRowsResponse(
            responseRows,
            value.BatchStatus.ToString(),
            value.RowsCount,
            value.ValidRowsCount,
            value.ErrorRowsCount,
            value.Version);

        return Ok(response);
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.user.cannot_edit"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.row.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.product_type.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.manufacturer.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.batch.rows_cannot_be_edited"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.invalid_status_transition"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.concurrency_conflict"
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось массово обновить строки импорта.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}