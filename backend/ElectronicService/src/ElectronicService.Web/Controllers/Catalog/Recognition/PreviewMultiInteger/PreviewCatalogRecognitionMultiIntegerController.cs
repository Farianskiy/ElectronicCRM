using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewMultiInteger;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/multi-integer-proposals")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionMultiIntegerController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(2097152)]
    public async Task<ActionResult<CatalogRecognitionMultiIntegerProposalSet>> Post(
        [FromBody] PreviewCatalogRecognitionMultiIntegerRequest request,
        [FromServices] CatalogRecognitionMultiIntegerProposalService service,
        CancellationToken cancellationToken)
    {
        var result = await service.PreviewAsync(
            request.ManufacturerId,
            request.ProductTypeId,
            request.CharacteristicDefinitionIds,
            request.ProductNames,
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Code switch
        {
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.selection_too_large" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось построить составные числовые шаблоны.",
            Detail = result.Error.Message
        });
    }
}