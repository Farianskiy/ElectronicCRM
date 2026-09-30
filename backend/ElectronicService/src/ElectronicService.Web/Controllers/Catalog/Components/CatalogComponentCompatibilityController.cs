using ElectronicService.Contracts.Catalog.Components;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Domain.Catalog.Components;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Components;

[ApiController]
[Route("api/catalog/component-compatibility")]
public sealed class CatalogComponentCompatibilityController : ControllerBase
{
    private readonly ICatalogComponentCompatibilityService _service;

    public CatalogComponentCompatibilityController(
        ICatalogComponentCompatibilityService service)
    {
        _service = service;
    }

    [HttpPost("product-types/{mainProductTypeCode}/needs")]
    [PermissionAuthorize(UserPermissionCode.DictionariesManage)]
    public async Task<ActionResult<ComponentNeedResponse>> CreateNeed(
        string mainProductTypeCode,
        [FromBody] CreateComponentNeedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateNeedAsync(
            mainProductTypeCode,
            request.Code,
            request.Name,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new ComponentNeedResponse(
                result.Value.Id,
                result.Value.MainProductTypeId,
                result.Value.MainProductTypeCode,
                result.Value.MainProductTypeName,
                result.Value.Code,
                result.Value.Name));
    }

    [HttpGet("needs")]
    [PermissionAuthorize(UserPermissionCode.ProductsView)]
    public async Task<ActionResult<IReadOnlyCollection<ComponentNeedResponse>>>
        GetNeeds(
            [FromQuery] string? mainProductTypeCode,
            CancellationToken cancellationToken = default)
    {
        var result = await _service.GetNeedsAsync(
            mainProductTypeCode,
            cancellationToken).ConfigureAwait(false);

        return Ok(result.Select(need => new ComponentNeedResponse(
            need.Id,
            need.MainProductTypeId,
            need.MainProductTypeCode,
            need.MainProductTypeName,
            need.Code,
            need.Name)).ToList());
    }

    [HttpPost("products/{componentProductId:guid}/offers")]
    [PermissionAuthorize(UserPermissionCode.DictionariesManage)]
    public async Task<ActionResult<ComponentOfferResponse>> CreateOffer(
        Guid componentProductId,
        [FromBody] CreateComponentOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateOfferAsync(
            componentProductId,
            request.NeedDefinitionId,
            request.Constraints.Select(constraint =>
                new ComponentCompatibilityConstraintInput(
                    constraint.CharacteristicDefinitionId,
                    constraint.Value)).ToList(),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new ComponentOfferResponse(
                result.Value.Id,
                result.Value.ComponentProductId,
                result.Value.NeedDefinitionId,
                result.Value.ConstraintsCount));
    }

    [HttpGet("products/{componentProductId:guid}/offers")]
    [PermissionAuthorize(UserPermissionCode.ProductsView)]
    public async Task<
        ActionResult<IReadOnlyCollection<ComponentOfferSummaryResponse>>>
        GetOffers(
            Guid componentProductId,
            CancellationToken cancellationToken = default)
    {
        var result = await _service.GetOffersAsync(
            componentProductId,
            cancellationToken).ConfigureAwait(false);

        return Ok(result.Select(offer => new ComponentOfferSummaryResponse(
            offer.Id,
            offer.NeedDefinitionId,
            offer.NeedCode,
            offer.NeedName,
            offer.MainProductTypeCode,
            offer.ConstraintsCount)).ToList());
    }

    [HttpPut("products/{productId:guid}/needs/{needDefinitionId:guid}")]
    [PermissionAuthorize(UserPermissionCode.ProductsEdit)]
    public async Task<IActionResult> SetNeedStatus(
        Guid productId,
        Guid needDefinitionId,
        [FromBody] SetProductNeedStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ProductNeedStatus>(
                request.Status,
                ignoreCase: true,
                out var status)
            || !Enum.IsDefined(status))
        {
            return BadRequest(
                "Статус должен быть Unknown, Missing, Included или NotApplicable.");
        }

        var result = await _service.SetNeedStatusAsync(
            productId,
            needDefinitionId,
            status,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error.Message);
    }

    [HttpPost("products/{productId:guid}/selected-components")]
    [PermissionAuthorize(UserPermissionCode.ProductsEdit)]
    public async Task<ActionResult<SelectedComponentResponse>> SelectComponent(
        Guid productId,
        [FromBody] SelectProductComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.SelectComponentAsync(
            productId,
            request.NeedDefinitionId,
            request.ComponentProductId,
            request.Quantity,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            MapSelectedComponent(result.Value));
    }

    [HttpPut("products/{productId:guid}/selected-components/{selectionId:guid}")]
    [PermissionAuthorize(UserPermissionCode.ProductsEdit)]
    public async Task<ActionResult<SelectedComponentResponse>> ChangeQuantity(
        Guid productId,
        Guid selectionId,
        [FromBody] ChangeSelectedComponentQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ChangeSelectedQuantityAsync(
            productId,
            selectionId,
            request.Quantity,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(MapSelectedComponent(result.Value))
            : BadRequest(result.Error.Message);
    }

    [HttpDelete("products/{productId:guid}/selected-components/{selectionId:guid}")]
    [PermissionAuthorize(UserPermissionCode.ProductsEdit)]
    public async Task<IActionResult> RemoveSelectedComponent(
        Guid productId,
        Guid selectionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.RemoveSelectedComponentAsync(
            productId,
            selectionId,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error.Message);
    }

    [HttpGet("products/{productId:guid}")]
    [PermissionAuthorize(UserPermissionCode.ProductsView)]
    public async Task<ActionResult<ProductComponentCompatibilityResponse>> Get(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetCompatibilityAsync(
            productId,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return NotFound(result.Error.Message);
        }

        return Ok(new ProductComponentCompatibilityResponse(
            result.Value.ProductId,
            result.Value.Needs.Select(need =>
                new ProductNeedCompatibilityResponse(
                    need.NeedDefinitionId,
                    need.Code,
                    need.Name,
                    need.Status.ToString(),
                    need.CompatibleComponents.Select(component =>
                        new CompatibleComponentResponse(
                            component.ProductId,
                            component.Article,
                            component.Name,
                            component.PriceAmount,
                            component.PriceCurrency)).ToList(),
                    need.SelectedComponents.Select(MapSelectedComponent)
                        .ToList())).ToList()));
    }

    private static SelectedComponentResponse MapSelectedComponent(
        SelectedComponentResult selection)
    {
        return new SelectedComponentResponse(
            selection.Id,
            selection.NeedDefinitionId,
            selection.ComponentProductId,
            selection.Article,
            selection.Name,
            selection.Quantity,
            selection.PriceAmount,
            selection.PriceCurrency,
            selection.LineTotalAmount);
    }
}
