namespace ElectronicService.Contracts.Catalog.Components;

public sealed record CreateComponentNeedRequest(string Code, string Name);

public sealed record ComponentNeedResponse(
    Guid Id,
    Guid MainProductTypeId,
    string MainProductTypeCode,
    string MainProductTypeName,
    string Code,
    string Name);

public sealed record ComponentCompatibilityConstraintRequest(
    Guid CharacteristicDefinitionId,
    string Value);

public sealed record CreateComponentOfferRequest(
    Guid NeedDefinitionId,
    IReadOnlyCollection<ComponentCompatibilityConstraintRequest> Constraints);

public sealed record ComponentOfferResponse(
    Guid Id,
    Guid ComponentProductId,
    Guid NeedDefinitionId,
    int ConstraintsCount);

public sealed record ComponentOfferSummaryResponse(
    Guid Id,
    Guid NeedDefinitionId,
    string NeedCode,
    string NeedName,
    string MainProductTypeCode,
    int ConstraintsCount);

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
    string PriceCurrency);

public sealed record ProductNeedCompatibilityResponse(
    Guid NeedDefinitionId,
    string Code,
    string Name,
    string Status,
    IReadOnlyCollection<CompatibleComponentResponse> CompatibleComponents,
    IReadOnlyCollection<SelectedComponentResponse> SelectedComponents);

public sealed record ProductComponentCompatibilityResponse(
    Guid ProductId,
    IReadOnlyCollection<ProductNeedCompatibilityResponse> Needs);
