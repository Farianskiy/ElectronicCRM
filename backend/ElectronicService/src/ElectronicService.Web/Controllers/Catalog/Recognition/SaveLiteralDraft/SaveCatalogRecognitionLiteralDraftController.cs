using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.SaveLiteralDraft;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.SaveLiteralDraft;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/literal-drafts")]
public sealed class SaveCatalogRecognitionLiteralDraftController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SaveCatalogRecognitionLiteralDraftResponse>> Post(
        [FromBody] SaveCatalogRecognitionLiteralDraftRequest request,
        [FromServices] SaveCatalogRecognitionLiteralDraftCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new SaveCatalogRecognitionLiteralDraftCommand(
            request.ManufacturerId,
            request.ProductTypeId,
            request.CharacteristicDefinitionId,
            request.Literal,
            request.NormalizedValue,
            request.GeneratorVersion);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(new SaveCatalogRecognitionLiteralDraftResponse(result.Value));
        }

        var statusCode = result.Error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось сохранить предложение.",
            Detail = result.Error.Message
        });
    }
}