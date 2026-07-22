using ElectronicService.Contracts.Catalog.Products.ProductTypeMigration;
using ElectronicService.Core.Catalog.Products.PreviewProductTypeMigration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ElectronicService.Web.Controllers.Catalog.Products
    .PreviewProductTypeMigration;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/products/{productId:guid}/" +
    "product-type-migration/preview")]
public sealed class
    PreviewProductTypeMigrationController
    : ControllerBase
{
    private const string ProductNotFoundCode =
        "catalog.product.not_found";

    private const string ProductTypeNotFoundCode =
        "catalog.product_type.not_found";

    private const string SameTypeCode =
        "catalog.product_type_migration." +
        "target_must_be_different";

    private const string DefinitionNotFoundCode =
        "catalog.characteristic_definition.not_found";

    private readonly
        PreviewProductTypeMigrationQueryHandler
        _handler;

    public PreviewProductTypeMigrationController(
        PreviewProductTypeMigrationQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ProductTypeMigrationPreviewResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<
        ProductTypeMigrationPreviewResponse>> Preview(
            Guid productId,
            [FromBody]
            PreviewProductTypeMigrationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query =
            new PreviewProductTypeMigrationQuery(
                productId,
                request.TargetProductTypeId);

        var result = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    ProductNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    ProductTypeNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(
                    result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    SameTypeCode,
                    StringComparison.Ordinal))
            {
                return BadRequest(
                    result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    DefinitionNotFoundCode,
                    StringComparison.Ordinal))
            {
                return Conflict(
                    result.Error.Message);
            }

            return BadRequest(
                result.Error.Message);
        }

        var preview = result.Value;

        var response =
            new ProductTypeMigrationPreviewResponse(
                preview.ProductId,
                preview.ProductVersion,

                preview.CurrentProductTypeId,
                preview.CurrentProductTypeCode,
                preview.CurrentProductTypeName,

                preview.TargetProductTypeId,
                preview.TargetProductTypeCode,
                preview.TargetProductTypeName,

                preview.CanApplyWithoutAdditionalValues,

                preview.PreservedCharacteristics
                    .Select(characteristic =>
                        new ProductTypeMigrationCharacteristicValueResponse(
                            characteristic.DefinitionId,
                            characteristic.Code,
                            characteristic.Name,
                            characteristic.DataType,
                            characteristic.Unit,
                            characteristic.Value))
                    .ToList(),

                preview.RemovedCharacteristics
                    .Select(characteristic =>
                        new ProductTypeMigrationCharacteristicValueResponse(
                            characteristic.DefinitionId,
                            characteristic.Code,
                            characteristic.Name,
                            characteristic.DataType,
                            characteristic.Unit,
                            characteristic.Value))
                    .ToList(),

                preview.MissingRequiredCharacteristics
                    .Select(characteristic =>
                        new ProductTypeMigrationMissingRequiredCharacteristicResponse(
                            characteristic.DefinitionId,
                            characteristic.Code,
                            characteristic.Name,
                            characteristic.DataType,
                            characteristic.Unit))
                    .ToList());

        return Ok(response);
    }
}