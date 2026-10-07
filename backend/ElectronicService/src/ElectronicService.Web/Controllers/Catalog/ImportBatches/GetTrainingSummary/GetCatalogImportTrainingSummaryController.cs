using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetTrainingSummary;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportTrainingSummaryController : ControllerBase
{
    [HttpGet("{batchId:guid}/training-summary")]
    public async Task<ActionResult<CatalogImportTrainingSummary>> Get(
        Guid batchId,
        [FromServices] ICatalogTrainingExampleManagement management,
        CancellationToken cancellationToken)
    {
        var result = await management
            .GetImportSummaryAsync(batchId, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            Response.Headers.CacheControl = "private, no-store";
            return Ok(result.Value);
        }

        return Failure(result.Error);
    }

    private ObjectResult Failure(DomainError error)
    {
        var status = error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new ProblemDetails
        {
            Status = status,
            Title = "Сводка обучения по импорту",
            Detail = error.Message
        });
    }
}
