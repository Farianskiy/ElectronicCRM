using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ProductComponentNeedState : AggregateRoot
{
    private ProductComponentNeedState(
        Guid id,
        Guid productId,
        Guid needDefinitionId,
        ProductNeedStatus status)
        : base(id)
    {
        ProductId = productId;
        NeedDefinitionId = needDefinitionId;
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private ProductComponentNeedState()
    {
    }

    public Guid ProductId { get; private set; }

    public Guid NeedDefinitionId { get; private set; }

    public ProductNeedStatus Status { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<ProductComponentNeedState, DomainError> Create(
        Guid productId,
        Guid needDefinitionId,
        ProductNeedStatus status)
    {
        if (productId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productId));
        }

        if (needDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(needDefinitionId));
        }

        if (!Enum.IsDefined(status))
        {
            return GeneralErrors.ValueIsInvalid(nameof(status));
        }

        return new ProductComponentNeedState(
            Guid.CreateVersion7(),
            productId,
            needDefinitionId,
            status);
    }

    public UnitResult<DomainError> ChangeStatus(ProductNeedStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(status)));
        }

        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
        return UnitResult.Success<DomainError>();
    }
}
