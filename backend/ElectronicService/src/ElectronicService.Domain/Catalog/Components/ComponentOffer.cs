using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentOffer : AggregateRoot
{
    private readonly List<ComponentCompatibilityConditionGroup> _conditionGroups = [];

    private ComponentOffer(
        Guid id,
        Guid componentProductId,
        Guid mainProductTypeId,
        Guid? needDefinitionId)
        : base(id)
    {
        ComponentProductId = componentProductId;
        MainProductTypeId = mainProductTypeId;
        NeedDefinitionId = needDefinitionId;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private ComponentOffer()
    {
    }

    public Guid ComponentProductId { get; private set; }

    public Guid MainProductTypeId { get; private set; }

    public Guid? NeedDefinitionId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ComponentCompatibilityConditionGroup> ConditionGroups =>
        _conditionGroups;

    public IEnumerable<ComponentCompatibilityConstraint> GetConstraints()
    {
        return _conditionGroups.SelectMany(group => group.Constraints);
    }

    public static Result<ComponentOffer, DomainError> Create(
        Guid componentProductId,
        Guid mainProductTypeId,
        Guid? needDefinitionId = null)
    {
        if (componentProductId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(componentProductId));
        }

        if (mainProductTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(mainProductTypeId));
        }

        if (needDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(needDefinitionId));
        }

        var offer = new ComponentOffer(
            Guid.CreateVersion7(),
            componentProductId,
            mainProductTypeId,
            needDefinitionId);

        var groupResult = offer.AddConditionGroup();

        return groupResult.IsSuccess
            ? offer
            : Result.Failure<ComponentOffer, DomainError>(groupResult.Error);
    }

    public Result<ComponentCompatibilityConditionGroup, DomainError>
        AddConditionGroup()
    {
        var groupResult = ComponentCompatibilityConditionGroup.Create(
            Id,
            _conditionGroups.Count);

        if (groupResult.IsFailure)
        {
            return groupResult;
        }

        _conditionGroups.Add(groupResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;
        return groupResult;
    }

    public void ClearConditionGroups()
    {
        _conditionGroups.Clear();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public UnitResult<DomainError> AddConstraint(
        Guid characteristicDefinitionId,
        CharacteristicValue expectedValue,
        ComponentCompatibilityComparisonOperator comparisonOperator =
            ComponentCompatibilityComparisonOperator.Equal)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);

        var group = _conditionGroups.OrderBy(item => item.Position).FirstOrDefault();

        if (group is null)
        {
            var groupResult = AddConditionGroup();

            if (groupResult.IsFailure)
            {
                return UnitResult.Failure(groupResult.Error);
            }

            group = groupResult.Value;
        }

        return group.AddConstraint(
            characteristicDefinitionId,
            expectedValue,
            comparisonOperator);
    }
}
