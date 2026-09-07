using ElectronicService.Contracts.Catalog.Products.StockImport;
using ElectronicService.Core.Catalog.Products.StockImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.ImportStock;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/products/stock-import")]
public sealed class CatalogStockImportController : ControllerBase
{
    private const long MaximumFileSizeBytes = 20 * 1024 * 1024;
    private const long MaximumMultipartRequestSizeBytes =
        MaximumFileSizeBytes + 1_048_576;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumMultipartRequestSizeBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MaximumMultipartRequestSizeBytes)]
    [ProducesResponseType(
        typeof(ImportCatalogStockResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportCatalogStockResponse>> Import(
        [FromForm] Guid manufacturerId,
        [FromForm] IFormFile? file,
        [FromServices] ICatalogStockWorkbookImporter importer,
        CancellationToken cancellationToken)
    {
        if (manufacturerId == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Производитель не выбран.");
        }

        if (file is null || file.Length == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Файл остатков не передан.");
        }

        if (file.Length > MaximumFileSizeBytes)
        {
            return Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Файл слишком большой.",
                detail: "Максимальный размер файла — 20 МБ.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await importer.ImportAsync(
                    manufacturerId,
                    stream,
                    file.FileName,
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new ImportCatalogStockResponse(
                result.ReadRowsCount,
                result.MatchedRowsCount,
                result.UpdatedProductsCount,
                result.SkippedRowsCount,
                result.Issues.Select(issue =>
                    new ImportCatalogStockIssueResponse(
                        issue.RowNumber,
                        issue.Article,
                        issue.Message))
                    .ToList()));
        }
        catch (InvalidDataException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Не удалось загрузить остатки.",
                detail: exception.Message);
        }
    }
}
