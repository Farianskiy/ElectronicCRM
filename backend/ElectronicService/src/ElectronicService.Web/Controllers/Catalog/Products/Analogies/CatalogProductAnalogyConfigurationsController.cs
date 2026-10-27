using ElectronicService.Contracts.Catalog.Products.Analogies;
using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Core.Users.Access;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.Analogies;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/products/analogies/configurations")]
public sealed class CatalogProductAnalogyConfigurationsController(
    CatalogProductAnalogyConfigurationService service,
    CatalogProductAnalogyEvaluationService evaluation) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogProductAnalogyConfigurationWorkspaceResponse>> Get(
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetWorkspaceAsync(
            manufacturerId, productTypeId, cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : Problem(result.Error.Message, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost]
    public async Task<ActionResult<CatalogProductAnalogyConfigurationResponse>> Create(
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromBody] SaveCatalogProductAnalogyConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var settings = Map(request.Settings);
        if (settings is null)
        {
            return BadRequest("Проверьте режимы характеристик.");
        }

        var result = await service.CreateDraftAsync(
            manufacturerId, productTypeId, request.Name, settings, userId,
            cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : Failure(result.Error.Code, result.Error.Message);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CatalogProductAnalogyConfigurationResponse>> Update(
        Guid id,
        [FromBody] SaveCatalogProductAnalogyConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ExpectedVersion.HasValue)
        {
            return BadRequest("Не указана версия черновика.");
        }

        var settings = Map(request.Settings);
        if (settings is null)
        {
            return BadRequest("Проверьте режимы характеристик.");
        }

        var result = await service.UpdateDraftAsync(
            id, request.Name, settings, request.ExpectedVersion.Value,
            cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : Failure(result.Error.Code, result.Error.Message);
    }

    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<CatalogProductAnalogyConfigurationResponse>> Clone(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CreateFromVersionAsync(id, userId, cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : Failure(result.Error.Code, result.Error.Message);
    }

    [HttpPost("{id:guid}/evaluation")]
    public async Task<ActionResult<CatalogProductAnalogyEvaluationReportResponse>> Evaluate(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId)) return Unauthorized();
        var result = await evaluation.EvaluateAsync(id, userId, cancellationToken);
        return result.IsSuccess
            ? Ok(Map(result.Value))
            : Failure(result.Error.Code, result.Error.Message);
    }

    [HttpGet("{id:guid}/evaluation-reports")]
    public async Task<ActionResult<IReadOnlyCollection<CatalogProductAnalogyEvaluationReportResponse>>> Reports(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await evaluation.ListAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value.Select(Map).ToArray())
            : Failure(result.Error.Code, result.Error.Message);
    }

    [HttpGet("evaluation-reports/{reportId:guid}")]
    public async Task<ActionResult<CatalogProductAnalogyEvaluationReportDetailResponse>> Report(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var result = await evaluation.GetAsync(reportId, cancellationToken);
        return result.IsSuccess
            ? Ok(new CatalogProductAnalogyEvaluationReportDetailResponse(
                Map(result.Value.Report),
                result.Value.Rows.Select(x => new CatalogProductAnalogyEvaluationRowResponse(
                    x.ProductId, x.ProductName, x.CharacteristicCode, x.ExpectedValue,
                    x.BaselineValue, x.CandidateValue, x.BaselineCorrect,
                    x.CandidateCorrect)).ToArray()))
            : Failure(result.Error.Code, result.Error.Message);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<CatalogProductAnalogyActivationResponse>> Activate(
        Guid id,
        [FromBody] ActivateCatalogProductAnalogyConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirmed) return BadRequest("Подтвердите включение версии.");
        var result = await evaluation.ActivateAsync(id, request.ReportId,
            request.ExpectedVersion, cancellationToken);
        return result.IsSuccess
            ? Ok(new CatalogProductAnalogyActivationResponse(result.Value.ConfigurationId,
                result.Value.VersionNumber, result.Value.Status, result.Value.ActivatedAtUtc))
            : Failure(result.Error.Code, result.Error.Message);
    }

    private ObjectResult Failure(string code, string message)
    {
        var status = code switch
        {
            "catalog_analogy.concurrency" => StatusCodes.Status409Conflict,
            "catalog_analogy.not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(message, statusCode: status);
    }

    private static CatalogProductAnalogySettings? Map(
        CatalogProductAnalogySettingsContract source)
    {
        var characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal);
        foreach (var item in source.Characteristics)
        {
            if (!Enum.TryParse<CatalogProductAnalogyMode>(item.Value.Mode, true, out var mode))
            {
                return null;
            }
            characteristics[item.Key] = new(mode, item.Value.MinimumSupportingProducts, item.Value.MinimumSupportRatio);
        }
        return new(source.MinimumSupportingProducts, source.MinimumProductsWithCharacteristic,
            source.MinimumSupportRatio, source.MaximumAlternativeValues,
            source.MaximumCandidatePoolSize, characteristics);
    }

    private static CatalogProductAnalogyConfigurationWorkspaceResponse Map(
        CatalogProductAnalogyConfigurationWorkspace source) =>
        new(Map(source.Active), source.Versions.Select(Map).ToArray());

    private static CatalogProductAnalogyConfigurationResponse Map(
        CatalogProductAnalogyConfigurationView source) =>
        new(source.Id, source.VersionNumber, source.Name, source.Status,
            source.SettingsHash, source.IsSystem, Map(source.Settings),
            source.CreatedAtUtc, source.UpdatedAtUtc, source.Version);

    private static CatalogProductAnalogySettingsContract Map(
        CatalogProductAnalogySettings source) =>
        new(source.MinimumSupportingProducts, source.MinimumProductsWithCharacteristic,
            source.MinimumSupportRatio, source.MaximumAlternativeValues,
            source.MaximumCandidatePoolSize,
            source.Characteristics.ToDictionary(x => x.Key,
                x => new CatalogProductAnalogyCharacteristicSettingsContract(
                    x.Value.Mode.ToString(), x.Value.MinimumSupportingProducts,
                    x.Value.MinimumSupportRatio), StringComparer.Ordinal));

    private static CatalogProductAnalogyEvaluationReportResponse Map(
        CatalogProductAnalogyEvaluationReportView source) =>
        new(source.Id, source.ConfigurationId, source.CreatedAtUtc,
            source.ControlProducts, source.ControlValues, source.Improvements,
            source.Regressions, source.CandidateCorrect, source.CandidateIncorrect,
            source.IsReady, source.IsCurrent, source.BlockingReasons);
}
