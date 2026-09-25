using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Preview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Preview;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/recognition/preview")]
public sealed class CatalogProductNameRecognitionPreviewController : ControllerBase
{
    private readonly PreviewCatalogProductNameRecognitionQueryHandler _handler;

    public CatalogProductNameRecognitionPreviewController(PreviewCatalogProductNameRecognitionQueryHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CatalogProductNameRecognitionPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CatalogProductNameRecognitionPreviewResponse>> Preview(
        [FromBody] PreviewCatalogProductNameRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest("Наименование товара обязательно.");
        }

        var query = new PreviewCatalogProductNameRecognitionQuery(
            request.ProductName,
            request.ProductTypeCode,
            request.ManufacturerId);

        var handled = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (handled.IsFailure)
        {
            var status = handled.Error.Code switch
            {
                "recognition.product_type_not_found" or "recognition.manufacturer_not_found" => StatusCodes.Status404NotFound,
                "recognition.scope_mismatch" => StatusCodes.Status409Conflict,
                "recognition.active_rule_invalid" or "recognition.timeout" => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest
            };
            var problem = new ProblemDetails
            {
                Status = status,
                Title = "Распознавание действующей конфигурацией не выполнено.",
                Detail = handled.Error.Message
            };
            problem.Extensions["code"] = handled.Error.Code;
            return StatusCode(status, problem);
        }

        var result = handled.Value;
        var response = new CatalogProductNameRecognitionPreviewResponse(
            result.ProductTypeId.HasValue,
            result.ProductTypeId,
            result.ProductTypeCode,
            result.ProductTypeName,
            result.AllowedCharacteristicCodes,
            result.RecognitionProfiles
                .Select(profile => new CatalogCharacteristicRecognitionProfileResponse(
                    profile.Id,
                    profile.ProductTypeId,
                    profile.CharacteristicDefinitionId,
                    profile.CharacteristicCode,
                    profile.CharacteristicName,
                    profile.StrategyKind.ToString(),
                    profile.Priority,
                    profile.MinimumConfidence,
                    profile.ConfigurationJson,
                    profile.IsActive,
                    profile.CreatedAtUtc,
                    profile.UpdatedAtUtc))
                .ToArray(),
            result.RecognitionResult.ProductName,
            result.RecognitionResult.NormalizedProductName,
            result.RecognitionResult.Characteristics
                .Select(MapCharacteristic)
                .ToArray(),
            result.RecognitionResult.Conflicts
                .Select(conflict => new CatalogRecognitionConflictResponse(
                    conflict.CharacteristicCode,
                    conflict.Candidates
                        .Select(MapCharacteristic)
                        .ToArray()))
                .ToArray(),
            result.RecognitionResult.Candidates
                .Select(MapCharacteristic)
                .ToArray(),
            result.ManufacturerId,
            result.ManufacturerName,
            result.HasCompleteScope,
            result.ActiveRuleSetVersionId,
            result.ActiveRuleSetSequenceNumber);

        return Ok(response);
    }

    private static CatalogRecognizedCharacteristicResponse MapCharacteristic(CatalogRecognizedCharacteristic characteristic)
    {
        return new CatalogRecognizedCharacteristicResponse(
            characteristic.CharacteristicCode,
            characteristic.RawValue,
            characteristic.NormalizedValue,
            characteristic.Confidence,
            characteristic.Source.ToString(),
            characteristic.StartIndex,
            characteristic.Length,
            characteristic.Priority,
            characteristic.RecognizerKey);
    }
}