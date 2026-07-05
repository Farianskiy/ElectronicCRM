using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ValueObjects;

public sealed class ProductName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 500;

    private ProductName()
    {
    }

    private ProductName(string value)
    {
        Value = value;
        NormalizedValue = Normalize(value);
    }

    public string Value { get; private set; } = string.Empty;

    public string NormalizedValue { get; private set; } = string.Empty;

    public static Result<ProductName, DomainError> Create(string value)
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

        return new ProductName(normalizedValue);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return NormalizedValue;
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}