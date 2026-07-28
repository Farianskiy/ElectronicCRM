using ElectronicService.Contracts.Catalog
    .ImportBatches;
using ElectronicService.Core.Catalog
    .ImportBatches.GetCatalogImportRows;
using ElectronicService.Domain.Catalog
    .ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ImportBatches.GetCatalogImportRows;

[ApiController]
[Authorize(Roles =
    "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class
    GetCatalogImportRowsController
    : ControllerBase
{
    [HttpGet("{batchId:guid}/rows")]
    [ProducesResponseType(
    typeof(GetCatalogImportRowsResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCatalogImportRowsResponse>> Get(
    Guid batchId,
    [FromServices] GetCatalogImportRowsQueryHandler handler,
    CancellationToken cancellationToken,
    [FromQuery] CatalogImportRowStatus? status = null,
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

        var query = new GetCatalogImportRowsQuery(
            batchId,
            currentUserId,
            status,
            page,
            pageSize);

        var result = await handler.Handle(
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var items = result.Value.Items
            .Select(item =>
                new CatalogImportRowResponse(
                    item.RowId,
                    item.RowNumber,
                    item.Status.ToString(),
                    item.RawData,
                    new CatalogImportNormalizedRowResponse(
                        item.Data.Name,
                        item.Data.Article,
                        item.Data.Manufacturer,
                        item.Data.ManufacturerId,
                        item.Data.Price,
                        item.Data.StockQuantity,
                        item.Data.Characteristics),
                    item.Issues
                        .Select(issue =>
                            new CatalogImportRowIssueResponse(
                                issue.Code,
                                issue.Message,
                                issue.Field,
                                issue.SourceColumnNumber))
                        .ToArray(),
                    item.Warnings
                        .Select(warning =>
                            new CatalogImportRowIssueResponse(
                                warning.Code,
                                warning.Message,
                                warning.Field,
                                warning.SourceColumnNumber))
                        .ToArray()))
            .ToArray();

        var response = new GetCatalogImportRowsResponse(
            items,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount,
            result.Value.TotalPages);

        return Ok(response);
    }

    private ObjectResult ToProblem(
        DomainError error)
    {
        var statusCode =
            error.Code switch
            {
                "catalog.import.batch.not_found"
                    => StatusCodes.Status404NotFound,

                "catalog.import.batch.access_denied"
                    => StatusCodes.Status403Forbidden,

                "catalog.import.current_user.not_found"
                    => StatusCodes.Status401Unauthorized,

                "catalog.import.rows.invalid_pagination"
                    => StatusCodes.Status400BadRequest,

                "catalog.import.row.invalid_json"
                    => StatusCodes.Status500InternalServerError,

                _ => StatusCodes.Status400BadRequest
            };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title =
                    "Не удалось получить строки " +
                    "пакета импорта.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}