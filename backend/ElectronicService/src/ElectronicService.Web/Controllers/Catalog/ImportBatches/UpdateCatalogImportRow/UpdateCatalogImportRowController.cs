using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportRow;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.UpdateCatalogImportRow;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class UpdateCatalogImportRowController : ControllerBase
{
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
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var command = new UpdateCatalogImportRowCommand(
            batchId,
            rowId,
            currentUserId,
            request.Name,
            request.Article,
            request.ManufacturerId,
            request.Price,
            request.StockQuantity,
            request.Characteristics ?? new Dictionary<string, string>(StringComparer.Ordinal));

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var value = result.Value;

        var data = new CatalogImportNormalizedRowResponse(
            value.Data.Name,
            value.Data.Article,
            value.Data.Manufacturer,
            value.Data.ManufacturerId,
            value.Data.Price,
            value.Data.StockQuantity,
            value.Data.Characteristics);

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

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось обновить строку импорта.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}