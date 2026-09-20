using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.SaveMultiIntegerDraft;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.SaveMultiIntegerDraft;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/multi-integer-drafts")]
public sealed class SaveCatalogRecognitionMultiIntegerDraftController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(2097152)]
    public async Task<ActionResult<SaveCatalogRecognitionMultiIntegerDraftResponse>> Post(
        [FromBody] SaveCatalogRecognitionMultiIntegerDraftRequest request,
        [FromServices] SaveCatalogRecognitionMultiIntegerDraftCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.Parts is null ||
            request.Parts.Count == 0 ||
            request.Parts.Count > 256 ||
            request.Parts.Any(part => part is null))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Некорректный шаблон.",
                Detail = "Передайте от 1 до 256 непустых частей шаблона."
            });
        }

        var parts = request.Parts
            .Select(part => new CatalogRecognitionMultiIntegerPart(
                part.Literal,
                part.CharacteristicDefinitionId))
            .ToArray();

        var command = new SaveCatalogRecognitionMultiIntegerDraftCommand(
            request.ManufacturerId,
            request.ProductTypeId,
            request.CharacteristicDefinitionIds,
            request.ProductNames,
            request.GeneratorVersion,
            new CatalogRecognitionMultiIntegerPattern(parts));

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(new SaveCatalogRecognitionMultiIntegerDraftResponse(result.Value));
        }

        var statusCode = result.Error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.conflict" => StatusCodes.Status409Conflict,
            "training.selection_too_large" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось сохранить многочисловой шаблон.",
            Detail = result.Error.Message
        });
    }
}