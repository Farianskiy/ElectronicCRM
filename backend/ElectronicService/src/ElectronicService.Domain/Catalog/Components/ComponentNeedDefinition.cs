using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentNeedDefinition : AggregateRoot
{
    public const int MaximumDefaultQuantityPerUnit = 1_000_000;

    private ComponentNeedDefinition(
        Guid id,
        Guid mainProductTypeId,
        string code,
        string name,
        ComponentNeedRequirementMode requirementMode,
        int defaultQuantityPerUnit)
        : base(id)
    {
        MainProductTypeId = mainProductTypeId;
        Code = code;
        Name = name;
        RequirementMode = requirementMode;
        DefaultQuantityPerUnit = defaultQuantityPerUnit;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private ComponentNeedDefinition()
    {
    }

    public Guid MainProductTypeId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ComponentNeedRequirementMode RequirementMode { get; private set; }

    public int DefaultQuantityPerUnit { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public UnitResult<DomainError> ChangeSettings(
        ComponentNeedRequirementMode requirementMode,
        int defaultQuantityPerUnit)
    {
        if (!Enum.IsDefined(requirementMode))
        {
            return GeneralErrors.ValueIsInvalid(nameof(requirementMode));
        }

        if (defaultQuantityPerUnit is < 1 or > MaximumDefaultQuantityPerUnit)
        {
            return GeneralErrors.ValueIsInvalid(nameof(defaultQuantityPerUnit));
        }

        RequirementMode = requirementMode;
        DefaultQuantityPerUnit = defaultQuantityPerUnit;

        return UnitResult.Success<DomainError>();
    }

    public static Result<ComponentNeedDefinition, DomainError> Create(
        Guid mainProductTypeId,
        string code,
        string name,
        ComponentNeedRequirementMode requirementMode =
            ComponentNeedRequirementMode.Optional,
        int defaultQuantityPerUnit = 1)
    {
        if (mainProductTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(mainProductTypeId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired(nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return GeneralErrors.ValueIsRequired(nameof(name));
        }

        if (!Enum.IsDefined(requirementMode))
        {
            return GeneralErrors.ValueIsInvalid(nameof(requirementMode));
        }

        if (defaultQuantityPerUnit is < 1 or > MaximumDefaultQuantityPerUnit)
        {
            return GeneralErrors.ValueIsInvalid(nameof(defaultQuantityPerUnit));
        }

        var normalizedCode = code.Trim().ToUpperInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
        var normalizedName = name.Trim();

        if (normalizedCode.Length > 100)
        {
            return GeneralErrors.ValueIsTooLong(nameof(code), 100);
        }

        if (normalizedName.Length > 200)
        {
            return GeneralErrors.ValueIsTooLong(nameof(name), 200);
        }

        return new ComponentNeedDefinition(
            Guid.CreateVersion7(),
            mainProductTypeId,
            normalizedCode,
            normalizedName,
            requirementMode,
            defaultQuantityPerUnit);
    }
}
