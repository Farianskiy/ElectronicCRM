using ElectronicService.Contracts.Catalog.Products.ProductTypeMigration;
using ElectronicService.Core.Catalog.Products.ApplyProductTypeMigration;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products
    .ApplyProductTypeMigration;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/products/{productId:guid}/" +
    "product-type-migration")]
public sealed class
    ApplyProductTypeMigrationController
    : ControllerBase
{
    private const string ProductNotFoundCode =
        "catalog.product.not_found";

    private const string ProductTypeNotFoundCode =
        "catalog.product_type.not_found";

    private const string DefinitionNotFoundCode =
        "catalog.characteristic_definition.not_found";

    private const string PreviewIsStaleCode =
        "catalog.product_type_migration.preview_is_stale";

    private const string ProductConcurrencyConflictCode =
        "catalog.product.concurrency_conflict";

    private readonly
        ApplyProductTypeMigrationCommandHandler
        _handler;

    public ApplyProductTypeMigrationController(
        ApplyProductTypeMigrationCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply(
        Guid productId,
        [FromBody]
        ApplyProductTypeMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!User.TryGetUserId(
        out var changedByUserId))
        {
            return Unauthorized();
        }

        var requiredValues =
            request.RequiredValues?
                .Select(value =>
                    new ApplyProductTypeMigrationValueCommand(
                        value.DefinitionId,
                        value.Value))
                .ToList()
            ?? [];

        var command =
            new ApplyProductTypeMigrationCommand(
                productId,
                changedByUserId,

                request.TargetProductTypeId,
                request.ExpectedProductVersion,
                request.ExpectedCurrentProductTypeId,

                request
                    .ExpectedRemovedCharacteristicDefinitionIds
                ?? [],

                request
                    .ExpectedMissingRequiredCharacteristicDefinitionIds
                ?? [],

                requiredValues);

        var result = await _handler
            .Handle(command, cancellationToken)
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
                PreviewIsStaleCode,
                StringComparison.Ordinal)
            || string.Equals(
                result.Error.Code,
                DefinitionNotFoundCode,
                StringComparison.Ordinal)
            || string.Equals(
                result.Error.Code,
                ProductConcurrencyConflictCode,
                StringComparison.Ordinal))
            {
                return Conflict(
                    result.Error.Message);
            }

            return BadRequest(
                result.Error.Message);
        }

        return NoContent();
    }
}