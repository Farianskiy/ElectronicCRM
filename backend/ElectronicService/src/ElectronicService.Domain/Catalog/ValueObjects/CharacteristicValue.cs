using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ValueObjects;

public sealed class CharacteristicValue : ValueObject
{
    private CharacteristicValue()
    {
    }

    private CharacteristicValue(
        CharacteristicDataType dataType,
        string? textValue,
        decimal? numberValue,
        bool? booleanValue)
    {
        DataType = dataType;
        TextValue = textValue;
        NumberValue = numberValue;
        BooleanValue = booleanValue;
    }

    public CharacteristicDataType DataType { get; private set; }

    public string? TextValue { get; private set; }

    public decimal? NumberValue { get; private set; }

    public bool? BooleanValue { get; private set; }

    // Создаёт текстовое значение
    public static Result<CharacteristicValue, DomainError> CreateText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired(nameof(value));
        }

        return new CharacteristicValue(
            CharacteristicDataType.Text,
            value.Trim(),
            null,
            null);
    }

    // Создаёт числовое значение
    public static Result<CharacteristicValue, DomainError> CreateNumber(decimal value)
    {
        return new CharacteristicValue(
            CharacteristicDataType.Number,
            null,
            value,
            null);
    }

    // Создаёт булевое значение
    public static Result<CharacteristicValue, DomainError> CreateBoolean(bool value)
    {
        return new CharacteristicValue(
            CharacteristicDataType.Boolean,
            null,
            null,
            value);
    }

    // Возвращает значение как object
    // Это может пригодиться при DTO, выводе в API или сравнении
    public object? RawValue
    {
        get
        {
            return DataType switch
            {
                CharacteristicDataType.Text => TextValue,
                CharacteristicDataType.Number => NumberValue,
                CharacteristicDataType.Boolean => BooleanValue,
                _ => null
            };
        }
    }

    // Возвращает значение строкой
    public override string ToString()
    {
        return DataType switch
        {
            CharacteristicDataType.Text => TextValue ?? string.Empty,
            CharacteristicDataType.Number => NumberValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            CharacteristicDataType.Boolean => BooleanValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            _ => string.Empty
        };
    }

    // Это метод из ValueObject.
    //Он говорит, по каким полям сравнивать два значения
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DataType;

        if (TextValue is not null)
        {
            yield return TextValue.ToUpperInvariant();
        }

        if (NumberValue is not null)
        {
            yield return NumberValue.Value;
        }

        if (BooleanValue is not null)
        {
            yield return BooleanValue.Value;
        }
    }
}