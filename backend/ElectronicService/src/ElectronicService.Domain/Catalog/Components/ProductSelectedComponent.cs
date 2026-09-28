using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ProductSelectedComponent : AggregateRoot
{
    public const int MaximumQuantity = 1_000_000;

    private ProductSelectedComponent(
        Guid id,
        Guid mainProductId,
        Guid needDefinitionId,
        Guid componentProductId,
        int quantity)
        : base(id)
    {
        MainProductId = mainProductId;
        NeedDefinitionId = needDefinitionId;
        ComponentProductId = componentProductId;
        Quantity = quantity;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private ProductSelectedComponent()
    {
    }

    public Guid MainProductId { get; private set; }

    public Guid NeedDefinitionId { get; private set; }

    public Guid ComponentProductId { get; private set; }

    public int Quantity { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Result<ProductSelectedComponent, DomainError> Create(
        Guid mainProductId,
        Guid needDefinitionId,
        Guid componentProductId,
        int quantity)
    {
        if (mainProductId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(mainProductId));
        }

        if (needDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(needDefinitionId));
        }

        if (componentProductId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(componentProductId));
        }

        if (mainProductId == componentProductId)
        {
            return ComponentCompatibilityErrors.ProductCannotSelectItself();
        }

        var quantityResult = ValidateQuantity(quantity);

        if (quantityResult.IsFailure)
        {
            return quantityResult.Error;
        }

        return new ProductSelectedComponent(
            Guid.CreateVersion7(),
            mainProductId,
            needDefinitionId,
            componentProductId,
            quantity);
    }

    public UnitResult<DomainError> ChangeQuantity(int quantity)
    {
        var quantityResult = ValidateQuantity(quantity);

        if (quantityResult.IsFailure)
        {
            return quantityResult;
        }

        Quantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
        return UnitResult.Success<DomainError>();
    }

    private static UnitResult<DomainError> ValidateQuantity(int quantity)
    {
        return quantity is >= 1 and <= MaximumQuantity
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure(
                ComponentCompatibilityErrors.InvalidSelectedQuantity(
                    MaximumQuantity));
    }
}
