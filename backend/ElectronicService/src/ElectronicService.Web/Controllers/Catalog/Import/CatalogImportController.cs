using ElectronicService.Contracts.Catalog.Import;
using ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Import;

[ApiController]
[Route("api/catalog/import")]
public sealed class CatalogImportController : ControllerBase
{
    private readonly ImportProductsFromExcelCommandHandler _handler;

    public CatalogImportController(ImportProductsFromExcelCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("excel")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportProductsFromExcelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportProductsFromExcelResponse>> ImportProductsFromExcel(
        [FromForm] ImportProductsFromExcelRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Excel file is required.");
        }

        var originalFileName = Path.GetFileName(request.File.FileName);

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return BadRequest("File name is required.");
        }

        var extension = Path.GetExtension(originalFileName);

        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .xlsx files are supported.");
        }

        await using var fileStream = request.File.OpenReadStream();

        var command = new ImportProductsFromExcelCommand(
            fileStream,
            originalFileName);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new ImportProductsFromExcelResponse(
            result.TotalRows,
            result.ImportedRows,
            result.SkippedRows,
            result.Errors));
    }
}