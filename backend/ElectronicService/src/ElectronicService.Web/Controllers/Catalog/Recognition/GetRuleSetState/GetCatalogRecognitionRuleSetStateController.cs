using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetRuleSetState;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-state")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionRuleSetStateController
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogRecognitionRuleSetState>> Get(
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromServices] ICatalogRecognitionActiveRuleSetReader reader,
        CancellationToken cancellationToken)
    {
        var result = await reader.GetStateAsync(
            manufacturerId,
            productTypeId,
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Не удалось прочитать состояние правил.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return BadRequest(problem);
    }
}