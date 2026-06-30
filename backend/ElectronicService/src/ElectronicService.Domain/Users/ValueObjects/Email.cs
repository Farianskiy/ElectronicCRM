using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Users.ValueObjects;

public sealed class Email : ValueObject
{
    private const int MaxLength = 320;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email, DomainError> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired(nameof(value));
        }

        var normalizedValue = value.Trim().ToUpperInvariant();

        if (normalizedValue.Length > MaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(value), MaxLength);
        }

        if (!IsValid(normalizedValue))
        {
            return GeneralErrors.ValueIsInvalid(nameof(value));
        }

        return new Email(normalizedValue);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsValid(string value)
    {
        var atIndex = value.IndexOf('@', StringComparison.Ordinal);

        return atIndex > 0 &&
            atIndex == value.LastIndexOf('@') &&
            atIndex < value.Length - 1 &&
            value.Contains('.', StringComparison.Ordinal);
    }
}