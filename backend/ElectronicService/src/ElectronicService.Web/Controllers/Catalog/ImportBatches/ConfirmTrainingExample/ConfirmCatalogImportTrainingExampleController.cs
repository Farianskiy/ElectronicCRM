using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.Recognition.ConfirmTrainingExample;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.ConfirmTrainingExample;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/import-batches")]
public sealed class ConfirmCatalogImportTrainingExampleController : ControllerBase
{
    [HttpPost("{batchId:guid}/rows/{rowId:guid}/characteristics/{characteristicId:guid}/training-example")]
    public async Task<ActionResult<ConfirmCatalogImportTrainingExampleResponse>> Post(
        Guid batchId,
        Guid rowId,
        Guid characteristicId,
        [FromBody] ConfirmCatalogImportTrainingExampleRequest request,
        [FromServices] ConfirmCatalogRecognitionTrainingExampleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmCatalogRecognitionTrainingExampleCommand(
            batchId,
            rowId,
            characteristicId,
            request.ProductName,
            request.ManufacturerId,
            request.ProductTypeId,
            request.NormalizedValue,
            request.RawValue,
            request.SpanStart,
            request.SpanLength);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(new ConfirmCatalogImportTrainingExampleResponse(result.Value));
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
            Title = "Не удалось подтвердить учебный пример.",
            Detail = result.Error.Message
        });
    }
}