using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentCompatibilityConstraint
    : ElectronicService.Domain.Abstractions.Entity
{
    private ComponentCompatibilityConstraint(
        Guid id,
        Guid conditionGroupId,
        Guid characteristicDefinitionId,
        ComponentCompatibilityComparisonOperator comparisonOperator,
        CharacteristicValue expectedValue)
        : base(id)
    {
        ConditionGroupId = conditionGroupId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        ComparisonOperator = comparisonOperator;
        ExpectedValue = expectedValue;
    }

    private ComponentCompatibilityConstraint()
    {
    }

    public Guid ConditionGroupId { get; private set; }

    public Guid CharacteristicDefinitionId { get; private set; }

    public ComponentCompatibilityComparisonOperator ComparisonOperator { get; private set; }

    public CharacteristicValue ExpectedValue { get; private set; } = null!;

    public static Result<ComponentCompatibilityConstraint, DomainError> Create(
        Guid conditionGroupId,
        Guid characteristicDefinitionId,
        CharacteristicValue expectedValue,
        ComponentCompatibilityComparisonOperator comparisonOperator =
            ComponentCompatibilityComparisonOperator.Equal)
    {
        if (conditionGroupId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(conditionGroupId));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        ArgumentNullException.ThrowIfNull(expectedValue);

        if (!Enum.IsDefined(comparisonOperator))
        {
            return GeneralErrors.ValueIsInvalid(nameof(comparisonOperator));
        }

        if (comparisonOperator != ComponentCompatibilityComparisonOperator.Equal
            && expectedValue.DataType != CharacteristicDataType.Number)
        {
            return GeneralErrors.ValueIsInvalid(nameof(comparisonOperator));
        }

        return new ComponentCompatibilityConstraint(
            Guid.CreateVersion7(),
            conditionGroupId,
            characteristicDefinitionId,
            comparisonOperator,
            expectedValue);
    }
}
