using ElectronicService.Contracts.Catalog.Import;
using ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Import;

[ApiController]
[Route("api/catalog/import/excel/preview")]
public sealed class CatalogExcelImportPreviewController : ControllerBase
{
    private readonly PreviewProductsExcelImportCommandHandler _handler;

    public CatalogExcelImportPreviewController(PreviewProductsExcelImportCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PreviewProductsExcelImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PreviewProductsExcelImportResponse>> PreviewProductsFromExcel(
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

        var command = new PreviewProductsExcelImportCommand(
            fileStream,
            originalFileName);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new PreviewProductsExcelImportResponse(
            result.FileName,
            result.ProductTypeCode,
            result.ProductTypeName,
            result.TotalRows,
            result.CreateRows,
            result.DuplicateRows,
            result.ErrorRows,
            result.NormalizedManufacturerRows,
            result.NewManufacturerRows,
            result.RowsLimit,
            result.IsRowsTruncated,
            result.ManufacturerNormalizations.Select(item =>
                new PreviewProductsExcelImportManufacturerNormalizationResponse(
                    item.RawName,
                    item.NormalizedName,
                    item.RowsCount)).ToList(),
            result.Rows.Select(row =>
                new PreviewProductsExcelImportRowResponse(
                    row.RowNumber,
                    row.Action,
                    row.Article,
                    row.Name,
                    row.ProductTypeCode,
                    row.RawManufacturerName,
                    row.NormalizedManufacturerName,
                    row.ManufacturerAction,
                    row.Characteristics,
                    row.Warnings,
                    row.Errors)).ToList(),
            result.Errors));
    }
}