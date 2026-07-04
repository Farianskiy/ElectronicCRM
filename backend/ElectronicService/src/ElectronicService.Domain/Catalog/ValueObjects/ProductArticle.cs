using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ValueObjects;

public sealed class ProductArticle : ValueObject
{
    private const int MaxLength = 100;

    private ProductArticle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<ProductArticle, DomainError> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired(nameof(value));
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(value), MaxLength);
        }

        return new ProductArticle(normalizedValue);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }
}