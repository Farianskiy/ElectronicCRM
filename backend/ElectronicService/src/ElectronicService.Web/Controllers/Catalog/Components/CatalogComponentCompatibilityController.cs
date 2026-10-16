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
        if (!Enum.TryParse<ComponentNeedRequirementMode>(
                request.RequirementMode,
                true,
                out var requirementMode))
        {
            return BadRequest("Некорректная обязательность группы комплектующих.");
        }

        var result = await _service.CreateNeedAsync(
            mainProductTypeCode,
            request.Code,
            request.Name,
            requirementMode,
            request.DefaultQuantityPerUnit,
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
                result.Value.Name,
                result.Value.RequirementMode.ToString(),
                result.Value.DefaultQuantityPerUnit));
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
            need.Name,
            need.RequirementMode.ToString(),
            need.DefaultQuantityPerUnit)).ToList());
    }

    [HttpPut("needs/{needDefinitionId:guid}")]
    [PermissionAuthorize(UserPermissionCode.DictionariesManage)]
    public async Task<ActionResult<ComponentNeedResponse>> UpdateNeed(
        Guid needDefinitionId,
        [FromBody] UpdateComponentNeedRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ComponentNeedRequirementMode>(
                request.RequirementMode,
                true,
                out var requirementMode))
        {
            return BadRequest("Некорректная обязательность группы комплектующих.");
        }

        var result = await _service.UpdateNeedAsync(
            needDefinitionId,
            requirementMode,
            request.DefaultQuantityPerUnit,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new ComponentNeedResponse(
            result.Value.Id,
            result.Value.MainProductTypeId,
            result.Value.MainProductTypeCode,
            result.Value.MainProductTypeName,
            result.Value.Code,
            result.Value.Name,
            result.Value.RequirementMode.ToString(),
            result.Value.DefaultQuantityPerUnit));
    }

    [HttpPost("products/{componentProductId:guid}/offers")]
    [PermissionAuthorize(UserPermissionCode.DictionariesManage)]
    public async Task<ActionResult<ComponentOfferResponse>> CreateOffer(
        Guid componentProductId,
        [FromBody] CreateComponentOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseConstraints(request.Constraints, out var constraints))
        {
            return BadRequest("Некорректный оператор условия совместимости.");
        }

        var result = await _service.CreateOfferAsync(
            componentProductId,
            request.NeedDefinitionId,
            constraints,
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

    [HttpGet("rules")]
    [PermissionAuthorize(UserPermissionCode.ProductsView)]
    public async Task<
        ActionResult<IReadOnlyCollection<ComponentCompatibilityRuleResponse>>>
        GetRules(
            [FromQuery] string? mainProductTypeCode,
            CancellationToken cancellationToken = default)
    {
        var result = await _service.GetRulesAsync(
            mainProductTypeCode,
            cancellationToken).ConfigureAwait(false);

        return Ok(result.Select(MapRule).ToList());
    }

    [HttpPut("offers/{offerId:guid}")]
    [PermissionAuthorize(UserPermissionCode.DictionariesManage)]
    public async Task<ActionResult<ComponentCompatibilityRuleResponse>>
        UpdateOffer(
            Guid offerId,
            [FromBody] UpdateComponentOfferRequest request,
            CancellationToken cancellationToken = default)
    {
        var parsedGroups = new List<ComponentCompatibilityConditionGroupInput>();
        foreach (var group in request.ConditionGroups)
        {
            if (!TryParseConstraints(group.Constraints ?? [], out var constraints))
            {
                return BadRequest("Некорректный оператор условия совместимости.");
            }

            parsedGroups.Add(new ComponentCompatibilityConditionGroupInput(constraints));
        }

        var result = await _service.UpdateOfferAsync(
            offerId,
            request.IsActive,
            parsedGroups,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(MapRule(result.Value))
            : BadRequest(result.Error.Message);
    }

    [HttpDelete("offers/{offerId:guid}")]
    [PermissionAuthorize(UserPermissionCode.DictionariesManage)]
    public async Task<IActionResult> DeleteOffer(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteOfferAsync(
            offerId,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? NoContent()
            : NotFound(result.Error.Message);
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
                    need.RequirementMode.ToString(),
                    need.DefaultQuantityPerUnit,
                    need.Status.ToString(),
                    need.CompatibleComponents.Select(component =>
                        new CompatibleComponentResponse(
                            component.ProductId,
                            component.Article,
                            component.Name,
                            component.PriceAmount,
                            component.PriceCurrency,
                            component.MatchConditions.Select(condition =>
                                new ComponentCompatibilityConditionResponse(
                                    condition.CharacteristicName,
                                    condition.ActualValue,
                                    condition.ExpectedValues)).ToList())).ToList(),
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

    private static ComponentCompatibilityRuleResponse MapRule(
        ComponentCompatibilityRuleResult rule)
    {
        return new ComponentCompatibilityRuleResponse(
            rule.Id,
            rule.ComponentProductId,
            rule.ComponentArticle,
            rule.ComponentName,
            rule.NeedDefinitionId,
            rule.NeedCode,
            rule.NeedName,
            rule.MainProductTypeCode,
            rule.MainProductTypeName,
            rule.IsActive,
            rule.ConditionGroups.Select(group =>
                new ComponentCompatibilityRuleGroupResponse(
                    group.Id,
                    group.Position,
                    group.Constraints.Select(constraint =>
                        new ComponentCompatibilityRuleConstraintResponse(
                            constraint.CharacteristicDefinitionId,
                            constraint.CharacteristicName,
                            constraint.Value,
                            constraint.ComparisonOperator.ToString())).ToList())).ToList());
    }

    private static bool TryParseConstraints(
        IEnumerable<ComponentCompatibilityConstraintRequest> requests,
        out IReadOnlyCollection<ComponentCompatibilityConstraintInput> constraints)
    {
        var result = new List<ComponentCompatibilityConstraintInput>();
        foreach (var request in requests)
        {
            if (!Enum.TryParse<ComponentCompatibilityComparisonOperator>(
                    request.ComparisonOperator,
                    true,
                    out var comparisonOperator)
                || !Enum.IsDefined(comparisonOperator))
            {
                constraints = [];
                return false;
            }

            result.Add(new ComponentCompatibilityConstraintInput(
                request.CharacteristicDefinitionId,
                request.Value,
                comparisonOperator));
        }

        constraints = result;
        return true;
    }
}
