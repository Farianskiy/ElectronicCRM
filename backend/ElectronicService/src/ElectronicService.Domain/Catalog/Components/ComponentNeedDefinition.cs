using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public sealed class ComponentNeedDefinition : AggregateRoot
{
    private ComponentNeedDefinition(
        Guid id,
        Guid mainProductTypeId,
        string code,
        string name)
        : base(id)
    {
        MainProductTypeId = mainProductTypeId;
        Code = code;
        Name = name;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private ComponentNeedDefinition()
    {
    }

    public Guid MainProductTypeId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<ComponentNeedDefinition, DomainError> Create(
        Guid mainProductTypeId,
        string code,
        string name)
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
            normalizedName);
    }
}
