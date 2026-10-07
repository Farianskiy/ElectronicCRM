using ElectronicService.Contracts.Catalog.Components;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Components;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/component-compatibility/import")]
public sealed class CatalogComponentCompatibilityImportController
    : ControllerBase
{
    private const long MaximumFileSizeBytes = 10 * 1024 * 1024;
    private readonly ICatalogComponentCompatibilityWorkbookService _service;

    public CatalogComponentCompatibilityImportController(
        ICatalogComponentCompatibilityWorkbookService service)
    {
        _service = service;
    }

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        return File(
            _service.CreateTemplate(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "component-compatibility-template.xlsx");
    }

    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Preview(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Выберите непустой XLSX-файл.");
        }

        if (file.Length > MaximumFileSizeBytes
            || !string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Поддерживаются XLSX-файлы размером до 10 МБ.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _service
            .PreviewAsync(stream, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new PreviewComponentCompatibilityImportResponse(
            result.Value.ReadRowsCount,
            result.Value.ReadyRowsCount,
            result.Value.SkippedRowsCount,
            result.Value.Rows.Select(row =>
                new ComponentCompatibilityImportPreviewRowResponse(
                    row.RowNumber,
                    row.ComponentArticle,
                    row.MainProductType,
                    row.Need,
                    row.ComponentProductId,
                    row.NeedDefinitionId,
                    row.Status.ToString(),
                    row.Message,
                    row.Constraints.Select(constraint =>
                        new ComponentCompatibilityImportConstraintResponse(
                            constraint.CharacteristicDefinitionId,
                            constraint.CharacteristicName,
                            constraint.Value)).ToArray())).ToArray()));
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromBody] ApplyComponentCompatibilityImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rows = (request.Rows ?? [])
            .Select(row => new ApplyComponentCompatibilityImportRow(
                row.ComponentProductId,
                row.NeedDefinitionId,
                (row.Constraints ?? [])
                    .Select(constraint =>
                        new ComponentCompatibilityImportConstraint(
                            constraint.CharacteristicDefinitionId,
                            constraint.CharacteristicName,
                            constraint.Value))
                    .ToArray()))
            .ToArray();
        var result = await _service
            .ApplyAsync(rows, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(new ApplyComponentCompatibilityImportResponse(result.Value))
            : BadRequest(result.Error.Message);
    }
}
