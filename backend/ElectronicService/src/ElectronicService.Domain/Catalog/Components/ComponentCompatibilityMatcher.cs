using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.Catalog.Components;

public static class ComponentCompatibilityMatcher
{
    public static bool IsCompatible(
        IEnumerable<ComponentCompatibilityConstraint> constraints,
        IReadOnlyDictionary<Guid, CharacteristicValue> productValues)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(productValues);

        return constraints
            .GroupBy(constraint => constraint.CharacteristicDefinitionId)
            .All(group => productValues.TryGetValue(group.Key, out var actual)
                && group.Any(constraint => constraint.ExpectedValue == actual));
    }
}
