using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Components;

public sealed record ComponentCompatibilityConstraintInput(
    Guid CharacteristicDefinitionId,
    string Value);

public sealed record ComponentNeedResult(
    Guid Id,
    Guid MainProductTypeId,
    string MainProductTypeCode,
    string MainProductTypeName,
    string Code,
    string Name);

public sealed record ComponentOfferResult(
    Guid Id,
    Guid ComponentProductId,
    Guid NeedDefinitionId,
    int ConstraintsCount);

public sealed record ComponentOfferSummaryResult(
    Guid Id,
    Guid NeedDefinitionId,
    string NeedCode,
    string NeedName,
    string MainProductTypeCode,
    int ConstraintsCount);

public sealed record CompatibleComponentResult(
    Guid ProductId,
    string Article,
    string Name,
    decimal PriceAmount,
    string PriceCurrency);

public sealed record SelectedComponentResult(
    Guid Id,
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    string Article,
    string Name,
    int Quantity,
    decimal PriceAmount,
    string PriceCurrency,
    decimal LineTotalAmount);

public sealed record ProductNeedCompatibilityResult(
    Guid NeedDefinitionId,
    string Code,
    string Name,
    ProductNeedStatus Status,
    IReadOnlyCollection<CompatibleComponentResult> CompatibleComponents,
    IReadOnlyCollection<SelectedComponentResult> SelectedComponents);

public sealed record ProductComponentCompatibilityResult(
    Guid ProductId,
    IReadOnlyCollection<ProductNeedCompatibilityResult> Needs);

public interface ICatalogComponentCompatibilityService
{
    Task<Result<ComponentNeedResult, DomainError>> CreateNeedAsync(
        string mainProductTypeCode,
        string code,
        string name,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ComponentNeedResult>> GetNeedsAsync(
        string? mainProductTypeCode,
        CancellationToken cancellationToken = default);

    Task<Result<ComponentOfferResult, DomainError>> CreateOfferAsync(
        Guid componentProductId,
        Guid needDefinitionId,
        IReadOnlyCollection<ComponentCompatibilityConstraintInput> constraints,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ComponentOfferSummaryResult>> GetOffersAsync(
        Guid componentProductId,
        CancellationToken cancellationToken = default);

    Task<UnitResult<DomainError>> SetNeedStatusAsync(
        Guid productId,
        Guid needDefinitionId,
        ProductNeedStatus status,
        CancellationToken cancellationToken = default);

    Task<Result<SelectedComponentResult, DomainError>> SelectComponentAsync(
        Guid mainProductId,
        Guid needDefinitionId,
        Guid componentProductId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<Result<SelectedComponentResult, DomainError>> ChangeSelectedQuantityAsync(
        Guid mainProductId,
        Guid selectionId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<UnitResult<DomainError>> RemoveSelectedComponentAsync(
        Guid mainProductId,
        Guid selectionId,
        CancellationToken cancellationToken = default);

    Task<Result<ProductComponentCompatibilityResult, DomainError>>
        GetCompatibilityAsync(
            Guid productId,
            CancellationToken cancellationToken = default);
}
