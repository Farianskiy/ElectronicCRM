using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Products;

public sealed class ProductAlias : Abstractions.Entity
{
    private const int MaxLength = 500;

    private ProductAlias(
        Guid id,
        Guid productId,
        string value,
        string normalizedValue)
        : base(id)
    {
        ProductId = productId;
        Value = value;
        NormalizedValue = normalizedValue;
    }

    private ProductAlias()
    {
    }

    public Guid ProductId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string NormalizedValue { get; private set; } = string.Empty;

    public static Result<ProductAlias, DomainError> Create(Guid productId, string value)
    {
        if (productId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired(nameof(value));
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(value), MaxLength);
        }

        return new ProductAlias(
            Guid.CreateVersion7(),
            productId,
            normalizedValue,
            Normalize(normalizedValue));
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}