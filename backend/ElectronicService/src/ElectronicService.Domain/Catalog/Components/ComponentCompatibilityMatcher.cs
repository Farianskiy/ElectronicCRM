using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.Catalog.Components;

public static class ComponentCompatibilityMatcher
{
    public static bool IsCompatible(
        IEnumerable<ComponentCompatibilityConditionGroup> conditionGroups,
        IReadOnlyDictionary<Guid, CharacteristicValue> productValues)
    {
        ArgumentNullException.ThrowIfNull(conditionGroups);
        ArgumentNullException.ThrowIfNull(productValues);

        return conditionGroups.Any(group => IsGroupCompatible(
            group.Constraints,
            productValues));
    }

    public static IReadOnlyCollection<ComponentCompatibilityConditionGroup>
        GetMatchingGroups(
            IEnumerable<ComponentCompatibilityConditionGroup> conditionGroups,
            IReadOnlyDictionary<Guid, CharacteristicValue> productValues)
    {
        ArgumentNullException.ThrowIfNull(conditionGroups);
        ArgumentNullException.ThrowIfNull(productValues);

        return conditionGroups
            .Where(group => IsGroupCompatible(group.Constraints, productValues))
            .OrderBy(group => group.Position)
            .ToArray();
    }

    private static bool IsGroupCompatible(
        IEnumerable<ComponentCompatibilityConstraint> constraints,
        IReadOnlyDictionary<Guid, CharacteristicValue> productValues)
    {
        return constraints
            .GroupBy(constraint => constraint.CharacteristicDefinitionId)
            .All(group => productValues.TryGetValue(group.Key, out var actual)
                && IsCharacteristicCompatible(group, actual));
    }

    private static bool IsCharacteristicCompatible(
        IEnumerable<ComponentCompatibilityConstraint> constraints,
        CharacteristicValue actual)
    {
        var items = constraints.ToArray();
        var equalConstraints = items
            .Where(item => item.ComparisonOperator ==
                ComponentCompatibilityComparisonOperator.Equal)
            .ToArray();
        var equalsMatch = equalConstraints.Length == 0
            || equalConstraints.Any(item => item.ExpectedValue == actual);

        if (!equalsMatch)
        {
            return false;
        }

        if (actual.DataType != CharacteristicDataType.Number
            || !actual.NumberValue.HasValue)
        {
            return items.All(item => item.ComparisonOperator ==
                ComponentCompatibilityComparisonOperator.Equal);
        }

        return items.All(item => item.ComparisonOperator switch
        {
            ComponentCompatibilityComparisonOperator.Equal => true,
            ComponentCompatibilityComparisonOperator.GreaterThanOrEqual =>
                item.ExpectedValue.NumberValue.HasValue
                && actual.NumberValue.Value >= item.ExpectedValue.NumberValue.Value,
            ComponentCompatibilityComparisonOperator.LessThanOrEqual =>
                item.ExpectedValue.NumberValue.HasValue
                && actual.NumberValue.Value <= item.ExpectedValue.NumberValue.Value,
            _ => false
        });
    }
}
