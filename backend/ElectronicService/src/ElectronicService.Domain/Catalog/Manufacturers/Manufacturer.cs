using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Manufacturers;

public sealed class Manufacturer : AggregateRoot
{
    private const int NameMaxLength = 200;

    private Manufacturer(
        Guid id,
        string name,
        string normalizedName)
        : base(id)
    {
        Name = name;
        NormalizedName = normalizedName;
    }

    private Manufacturer()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public static Result<Manufacturer, DomainError> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return GeneralErrors.ValueIsRequired(nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > NameMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(name), NameMaxLength);
        }

        return new Manufacturer(
            Guid.CreateVersion7(),
            normalizedName,
            Normalize(name));
    }

    // Метод переименовывает производителя
    public UnitResult<DomainError> Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsRequired(nameof(name)));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > NameMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(name), NameMaxLength));
        }

        Name = normalizedName;
        NormalizedName = Normalize(name);

        return UnitResult.Success<DomainError>();
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}