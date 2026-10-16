using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentCompatibilityConditionGroup
    : ElectronicService.Domain.Abstractions.Entity
{
    private readonly List<ComponentCompatibilityConstraint> _constraints = [];

    private ComponentCompatibilityConditionGroup(
        Guid id,
        Guid componentOfferId,
        int position)
        : base(id)
    {
        ComponentOfferId = componentOfferId;
        Position = position;
    }

    private ComponentCompatibilityConditionGroup()
    {
    }

    public Guid ComponentOfferId { get; private set; }

    public int Position { get; private set; }

    public IReadOnlyCollection<ComponentCompatibilityConstraint> Constraints =>
        _constraints;

    public static Result<ComponentCompatibilityConditionGroup, DomainError> Create(
        Guid componentOfferId,
        int position)
    {
        if (componentOfferId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(componentOfferId));
        }

        if (position < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(position));
        }

        return new ComponentCompatibilityConditionGroup(
            Guid.CreateVersion7(),
            componentOfferId,
            position);
    }

    public UnitResult<DomainError> AddConstraint(
        Guid characteristicDefinitionId,
        CharacteristicValue expectedValue,
        ComponentCompatibilityComparisonOperator comparisonOperator =
            ComponentCompatibilityComparisonOperator.Equal)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);

        if (_constraints.Any(constraint =>
                constraint.CharacteristicDefinitionId == characteristicDefinitionId
                && constraint.ComparisonOperator == comparisonOperator
                && constraint.ExpectedValue == expectedValue))
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.ConstraintAlreadyExists(
                    characteristicDefinitionId));
        }

        var result = ComponentCompatibilityConstraint.Create(
            Id,
            characteristicDefinitionId,
            expectedValue,
            comparisonOperator);

        if (result.IsFailure)
        {
            return UnitResult.Failure(result.Error);
        }

        _constraints.Add(result.Value);
        return UnitResult.Success<DomainError>();
    }
}
