using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Characteristics;

public sealed class CharacteristicDefinition : AggregateRoot
{
    private const int CodeMaxLength = 100;
    private const int NameMaxLength = 200;
    private const int UnitMaxLength = 50;

    private CharacteristicDefinition(
        Guid id,
        string code,
        string name,
        CharacteristicDataType dataType,
        string? unit)
        : base(id)
    {
        Code = code;
        Name = name;
        DataType = dataType;
        Unit = unit;
    }

    private CharacteristicDefinition()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public CharacteristicDataType DataType { get; private set; }

    public string? Unit { get; private set; }

    public static Result<CharacteristicDefinition, DomainError> Create(
        string code,
        string name,
        CharacteristicDataType dataType,
        string? unit = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired(nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return GeneralErrors.ValueIsRequired(nameof(name));
        }

        var normalizedCode = NormalizeCode(code);
        var normalizedName = name.Trim();
        var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

        if (normalizedCode.Length > CodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(code), CodeMaxLength);
        }

        if (normalizedName.Length > NameMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(name), NameMaxLength);
        }

        if (normalizedUnit is not null && normalizedUnit.Length > UnitMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(unit), UnitMaxLength);
        }

        if (dataType == CharacteristicDataType.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(dataType));
        }

        return new CharacteristicDefinition(
            Guid.CreateVersion7(),
            normalizedCode,
            normalizedName,
            dataType,
            normalizedUnit);
    }

    // Метод проверяет, подходит ли значение под тип характеристики
    public UnitResult<DomainError> ValidateValue(CharacteristicValue value)
    {
        if (value.DataType != DataType)
        {
            return UnitResult.Failure(
                CatalogErrors.CharacteristicValueTypeMismatch(
                    Code,
                    DataType.ToString(),
                    value.DataType.ToString()));
        }

        return UnitResult.Success<DomainError>();
    }

    // Метод меняет человекочитаемое название характеристики
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

        return UnitResult.Success<DomainError>();
    }

    // Метод меняет единицу измерения характеристики
    public UnitResult<DomainError> ChangeUnit(string? unit)
    {
        var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

        if (normalizedUnit is not null && normalizedUnit.Length > UnitMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(unit), UnitMaxLength));
        }

        Unit = normalizedUnit;

        return UnitResult.Success<DomainError>();
    }

    private static string NormalizeCode(string code)
    {
        return code
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }
}