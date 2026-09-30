using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentOffer : AggregateRoot
{
    private readonly List<ComponentCompatibilityConstraint> _constraints = [];

    private ComponentOffer(
        Guid id,
        Guid componentProductId,
        Guid needDefinitionId)
        : base(id)
    {
        ComponentProductId = componentProductId;
        NeedDefinitionId = needDefinitionId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private ComponentOffer()
    {
    }

    public Guid ComponentProductId { get; private set; }

    public Guid NeedDefinitionId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<ComponentCompatibilityConstraint> Constraints =>
        _constraints;

    public static Result<ComponentOffer, DomainError> Create(
        Guid componentProductId,
        Guid needDefinitionId)
    {
        if (componentProductId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(componentProductId));
        }

        if (needDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(needDefinitionId));
        }

        return new ComponentOffer(
            Guid.CreateVersion7(),
            componentProductId,
            needDefinitionId);
    }

    public UnitResult<DomainError> AddConstraint(
        Guid characteristicDefinitionId,
        CharacteristicValue expectedValue)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);

        if (_constraints.Any(constraint =>
                constraint.CharacteristicDefinitionId == characteristicDefinitionId
                && constraint.ExpectedValue == expectedValue))
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.ConstraintAlreadyExists(
                    characteristicDefinitionId));
        }

        var result = ComponentCompatibilityConstraint.Create(
            Id,
            characteristicDefinitionId,
            expectedValue);

        if (result.IsFailure)
        {
            return UnitResult.Failure(result.Error);
        }

        _constraints.Add(result.Value);
        return UnitResult.Success<DomainError>();
    }
}
