using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.CreateRuleSetVersion;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.CreateRuleSetVersion;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-versions")]
public sealed class CreateCatalogRecognitionRuleSetVersionController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(131072)]
    public async Task<ActionResult<CreateCatalogRecognitionRuleSetVersionResponse>> Post(
        [FromBody] CreateCatalogRecognitionRuleSetVersionRequest request,
        [FromServices] CreateCatalogRecognitionRuleSetVersionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.Entries is null ||
            request.Entries.Count == 0 ||
            request.Entries.Count > 100 ||
            request.Entries.Any(entry =>
                entry is null ||
                entry.Kind < 1 ||
                entry.Kind > 3 ||
                entry.DraftId == Guid.Empty))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Передайте от 1 до 100 корректных ссылок на шаблоны."
            });
        }

        var entries = request.Entries
            .Select(entry => new CatalogRecognitionRuleSetEntryData(
                (CatalogRecognitionRuleKind)entry.Kind,
                entry.DraftId))
            .ToArray();

        var command = new CreateCatalogRecognitionRuleSetVersionCommand(
            request.ManufacturerId,
            request.ProductTypeId,
            request.Name,
            entries);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(new CreateCatalogRecognitionRuleSetVersionResponse(
                result.Value.Id,
                result.Value.VersionNumber));
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
            Title = "Не удалось создать версию правил.",
            Detail = result.Error.Message
        });
    }
}