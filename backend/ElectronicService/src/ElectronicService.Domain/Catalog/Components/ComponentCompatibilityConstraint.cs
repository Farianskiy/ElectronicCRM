using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentCompatibilityConstraint
    : ElectronicService.Domain.Abstractions.Entity
{
    private ComponentCompatibilityConstraint(
        Guid id,
        Guid componentOfferId,
        Guid characteristicDefinitionId,
        CharacteristicValue expectedValue)
        : base(id)
    {
        ComponentOfferId = componentOfferId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        ExpectedValue = expectedValue;
    }

    private ComponentCompatibilityConstraint()
    {
    }

    public Guid ComponentOfferId { get; private set; }

    public Guid CharacteristicDefinitionId { get; private set; }

    public CharacteristicValue ExpectedValue { get; private set; } = null!;

    public static Result<ComponentCompatibilityConstraint, DomainError> Create(
        Guid componentOfferId,
        Guid characteristicDefinitionId,
        CharacteristicValue expectedValue)
    {
        if (componentOfferId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(componentOfferId));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        ArgumentNullException.ThrowIfNull(expectedValue);

        return new ComponentCompatibilityConstraint(
            Guid.CreateVersion7(),
            componentOfferId,
            characteristicDefinitionId,
            expectedValue);
    }
}
