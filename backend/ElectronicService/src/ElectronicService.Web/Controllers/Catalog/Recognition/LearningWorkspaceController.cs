using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/learning-workspace")]
public sealed class LearningWorkspaceController(LearningWorkspaceReader reader) : ControllerBase
{
    [HttpGet("{section}")]
    public async Task<IActionResult> Read(string section, Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken,
        int page = 1, string status = "All", Guid? id = null, Guid? parentId = null)
    {
        Response.Headers.CacheControl = "private, no-store";
        var result = await reader.ReadAsync(manufacturerId, productTypeId, section, page, status, id, parentId, cancellationToken);
        if (result.IsSuccess) return Ok(result.Value);
        var code = result.Error.Code switch { "training.unauthorized" => 401, "training.forbidden" => 403, "training.not_found" => 404, _ => 400 };
        return StatusCode(code, new ProblemDetails { Status = code, Title = "Рабочее пространство обучения", Detail = result.Error.Message });
    }
}
