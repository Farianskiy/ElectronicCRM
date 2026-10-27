namespace ElectronicService.Contracts.Catalog.Components;

public sealed record CreateComponentNeedRequest(
    string? Code,
    string Name,
    string RequirementMode = "Optional",
    int DefaultQuantityPerUnit = 1);

public sealed record UpdateComponentNeedRequest(
    string RequirementMode,
    int DefaultQuantityPerUnit);

public sealed record ComponentNeedResponse(
    Guid Id,
    Guid MainProductTypeId,
    string MainProductTypeCode,
    string MainProductTypeName,
    string Code,
    string Name,
    string RequirementMode,
    int DefaultQuantityPerUnit);

public sealed record ComponentCompatibilityConstraintRequest(
    Guid CharacteristicDefinitionId,
    string Value,
    string ComparisonOperator = "Equal");

public sealed record ComponentCompatibilityConditionGroupRequest(
    IReadOnlyCollection<ComponentCompatibilityConstraintRequest>? Constraints);

public sealed record CreateComponentOfferRequest(
    Guid? NeedDefinitionId,
    string? MainProductTypeCode,
    IReadOnlyCollection<ComponentCompatibilityConstraintRequest> Constraints);

public sealed record UpdateComponentOfferRequest(
    bool IsActive,
    IReadOnlyCollection<ComponentCompatibilityConditionGroupRequest> ConditionGroups);

public sealed record ComponentOfferResponse(
    Guid Id,
    Guid ComponentProductId,
    Guid MainProductTypeId,
    Guid? NeedDefinitionId,
    int ConstraintsCount);

public sealed record ComponentOfferSummaryResponse(
    Guid Id,
    Guid MainProductTypeId,
    Guid? NeedDefinitionId,
    string? NeedCode,
    string? NeedName,
    string MainProductTypeCode,
    int ConstraintsCount);

public sealed record ComponentCompatibilityRuleConstraintResponse(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value,
    string ComparisonOperator);

public sealed record ComponentCompatibilityRuleGroupResponse(
    Guid Id,
    int Position,
    IReadOnlyCollection<ComponentCompatibilityRuleConstraintResponse> Constraints);

public sealed record ComponentCompatibilityRuleResponse(
    Guid Id,
    Guid ComponentProductId,
    string ComponentArticle,
    string ComponentName,
    Guid ComponentManufacturerId,
    string ComponentManufacturerName,
    Guid MainProductTypeId,
    Guid? NeedDefinitionId,
    string? NeedCode,
    string? NeedName,
    string MainProductTypeCode,
    string MainProductTypeName,
    bool IsActive,
    IReadOnlyCollection<ComponentCompatibilityRuleGroupResponse> ConditionGroups);

public sealed record SetProductNeedStatusRequest(string Status);

public sealed record SelectProductComponentRequest(
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    int Quantity);

public sealed record ChangeSelectedComponentQuantityRequest(int Quantity);

public sealed record SelectedComponentResponse(
    Guid Id,
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    string Article,
    string Name,
    int Quantity,
    decimal PriceAmount,
    string PriceCurrency,
    decimal LineTotalAmount);

public sealed record CompatibleComponentResponse(
    Guid ProductId,
    string Article,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    IReadOnlyCollection<ComponentCompatibilityConditionResponse> MatchConditions);

public sealed record ComponentCompatibilityConditionResponse(
    string CharacteristicName,
    string ActualValue,
    IReadOnlyCollection<string> ExpectedValues);

public sealed record ProductNeedCompatibilityResponse(
    Guid NeedDefinitionId,
    string Code,
    string Name,
    string RequirementMode,
    int DefaultQuantityPerUnit,
    string Status,
    IReadOnlyCollection<CompatibleComponentResponse> CompatibleComponents,
    IReadOnlyCollection<SelectedComponentResponse> SelectedComponents);

public sealed record ProductComponentCompatibilityResponse(
    Guid ProductId,
    IReadOnlyCollection<ProductNeedCompatibilityResponse> Needs,
    IReadOnlyCollection<CompatibleComponentResponse> Accessories);
