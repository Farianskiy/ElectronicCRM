using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.SaveIntegerDraft;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.SaveIntegerDraft;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/integer-drafts")]
public sealed class SaveCatalogRecognitionIntegerDraftController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(262144)]
    public async Task<ActionResult<SaveCatalogRecognitionIntegerDraftResponse>> Post(
        [FromBody] SaveCatalogRecognitionIntegerDraftRequest request,
        [FromServices] SaveCatalogRecognitionIntegerDraftCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new SaveCatalogRecognitionIntegerDraftCommand(
            request.ManufacturerId,
            request.ProductTypeId,
            request.CharacteristicDefinitionId,
            request.Prefix,
            request.Suffixes,
            request.GeneratorVersion);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(new SaveCatalogRecognitionIntegerDraftResponse(result.Value));
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
            Title = "Не удалось сохранить числовой шаблон.",
            Detail = result.Error.Message
        });
    }
}