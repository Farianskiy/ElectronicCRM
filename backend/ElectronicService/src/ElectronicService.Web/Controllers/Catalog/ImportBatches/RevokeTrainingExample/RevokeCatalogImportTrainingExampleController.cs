using ElectronicService.Core.Catalog.Recognition.RevokeTrainingExample;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.RevokeTrainingExample;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/import-batches")]
public sealed class RevokeCatalogImportTrainingExampleController : ControllerBase
{
    [HttpPost("{batchId:guid}/rows/{rowId:guid}/training-examples/{exampleId:guid}/revoke")]
    public async Task<IActionResult> Post(
        Guid batchId,
        Guid rowId,
        Guid exampleId,
        [FromServices] RevokeCatalogRecognitionTrainingExampleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RevokeCatalogRecognitionTrainingExampleCommand(batchId, rowId, exampleId), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        var statusCode = result.Error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось отозвать учебный пример.",
            Detail = result.Error.Message
        });
    }
}