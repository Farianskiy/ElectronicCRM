using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Users.ValueObjects;

public sealed class UserDisplayName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    private UserDisplayName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<UserDisplayName, DomainError> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired(nameof(value));
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length < MinLength)
        {
            return GeneralErrors.ValueIsTooShort(nameof(value), MinLength);
        }

        if (normalizedValue.Length > MaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(value), MaxLength);
        }

        return new UserDisplayName(normalizedValue);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return Value;
    }
}