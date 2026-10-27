using ElectronicService.Contracts.Catalog.Components;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.ImportBatches;
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
    private readonly ICatalogImportBatchRepository _importBatchRepository;

    public CatalogComponentCompatibilityImportController(
        ICatalogComponentCompatibilityWorkbookService service,
        ICatalogImportBatchRepository importBatchRepository)
    {
        _service = service;
        _importBatchRepository = importBatchRepository;
    }

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(
        [FromQuery] string? mainProductTypeCode,
        [FromQuery] string[]? characteristicCodes,
        [FromQuery] string[]? rangeCharacteristicCodes,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateTemplateAsync(
            mainProductTypeCode,
            characteristicCodes,
            rangeCharacteristicCodes,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return File(
            result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "component-compatibility-template.xlsx");
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportRules(
        [FromQuery] string mainProductTypeCode,
        [FromQuery] Guid[]? componentManufacturerIds,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ExportRulesAsync(
            mainProductTypeCode,
            componentManufacturerIds,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        var safeCode = string.Concat(mainProductTypeCode
            .Where(character => char.IsLetterOrDigit(character)
                || character is '-' or '_'));

        var scopeSuffix = componentManufacturerIds is { Length: > 0 }
            ? "-selected-manufacturers"
            : string.Empty;

        return File(
            result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"component-compatibility-{safeCode}{scopeSuffix}.xlsx");
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

        return Ok(ToPreviewResponse(result.Value));
    }

    [HttpPost("batches/{batchId:guid}/preview")]
    public async Task<IActionResult> PreviewFromImportBatch(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _importBatchRepository
            .GetByIdWithFileAsync(batchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return NotFound("Пакет импорта не найден.");
        }

        if (batch.Status != CatalogImportBatchStatus.Applied)
        {
            return BadRequest(
                "Сначала завершите импорт товаров, затем проверяйте правила совместимости.");
        }

        await using var stream = new MemoryStream(
            batch.File.Content.ToArray(),
            writable: false);
        var result = await _service
            .PreviewAsync(stream, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(ToPreviewResponse(result.Value))
            : BadRequest(result.Error.Message);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromBody] ApplyComponentCompatibilityImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasInvalidOperator = (request.Rows ?? [])
            .SelectMany(row => row.Constraints ?? [])
            .Any(constraint =>
                !Enum.TryParse<ComponentCompatibilityComparisonOperator>(
                    constraint.ComparisonOperator,
                    true,
                    out var comparisonOperator)
                || !Enum.IsDefined(comparisonOperator));

        if (hasInvalidOperator)
        {
            return BadRequest("Некорректный оператор условия совместимости.");
        }

        var rows = (request.Rows ?? [])
            .Select(row => new ApplyComponentCompatibilityImportRow(
                row.ComponentProductId,
                row.MainProductTypeId,
                row.NeedDefinitionId,
                row.IsActive,
                (row.Constraints ?? [])
                    .Select(constraint =>
                        new ComponentCompatibilityImportConstraint(
                            constraint.CharacteristicDefinitionId,
                            constraint.CharacteristicName,
                            constraint.Value,
                            Enum.Parse<ComponentCompatibilityComparisonOperator>(
                                constraint.ComparisonOperator,
                                true)))
                    .ToArray()))
            .ToArray();
        var result = await _service
            .ApplyAsync(
                rows,
                request.AllowRulesWithoutConditions,
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(new ApplyComponentCompatibilityImportResponse(result.Value))
            : BadRequest(result.Error.Message);
    }

    private static PreviewComponentCompatibilityImportResponse ToPreviewResponse(
        ComponentCompatibilityImportPreview preview)
    {
        return new PreviewComponentCompatibilityImportResponse(
            preview.ReadRowsCount,
            preview.ReadyRowsCount,
            preview.SkippedRowsCount,
            preview.Rows.Select(row =>
                new ComponentCompatibilityImportPreviewRowResponse(
                    row.RowNumber,
                    row.ComponentArticle,
                    row.MainProductType,
                    row.Need,
                    row.ComponentProductId,
                    row.MainProductTypeId,
                    row.NeedDefinitionId,
                    row.IsActive,
                    row.Status.ToString(),
                    row.Message,
                    row.Constraints.Select(constraint =>
                        new ComponentCompatibilityImportConstraintResponse(
                            constraint.CharacteristicDefinitionId,
                            constraint.CharacteristicName,
                            constraint.Value,
                            constraint.ComparisonOperator.ToString())).ToArray()))
                .ToArray());
    }
}
