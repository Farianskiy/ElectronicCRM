using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetMultiIntegerDrafts;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/multi-integer-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionMultiIntegerDraftsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogRecognitionMultiIntegerDraftPage>> GetPage(
        [FromServices] ICatalogRecognitionMultiIntegerDraftReader reader,
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите производителя и тип товара."
            });
        }

        if (page < 1 || page > 10000 || pageSize < 1 || pageSize > 50)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Номер страницы должен быть от 1 до 10000, размер страницы — от 1 до 50."
            });
        }

        var result = await reader.GetPageAsync(
            manufacturerId,
            productTypeId,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }
}