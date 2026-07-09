using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public sealed class CatalogDictionaryTerm : AggregateRoot
{
    private CatalogDictionaryTerm(
        Guid id,
        string phrase,
        string normalizedPhrase,
        CatalogDictionaryTermKind kind,
        string? targetCode,
        string targetValue,
        int priority,
        CatalogDictionaryTermStatus status,
        CatalogDictionaryTermSource source)
        : base(id)
    {
        Phrase = phrase;
        NormalizedPhrase = normalizedPhrase;
        Kind = kind;
        TargetCode = targetCode;
        TargetValue = targetValue;
        Priority = priority;
        Status = status;
        Source = source;
        CreatedAtUtc = DateTime.UtcNow;
        ApprovedAtUtc = status == CatalogDictionaryTermStatus.Approved
            ? DateTime.UtcNow
            : null;
    }

    private CatalogDictionaryTerm()
    {
    }

    public string Phrase { get; private set; } = null!;

    public string NormalizedPhrase { get; private set; } = null!;

    public CatalogDictionaryTermKind Kind { get; private set; }

    public string? TargetCode { get; private set; }

    public string TargetValue { get; private set; } = null!;

    public int Priority { get; private set; }

    public CatalogDictionaryTermStatus Status { get; private set; }

    public CatalogDictionaryTermSource Source { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public static Result<CatalogDictionaryTerm, DomainError> Create(
        string phrase,
        CatalogDictionaryTermKind kind,
        string? targetCode,
        string targetValue,
        int priority,
        CatalogDictionaryTermStatus status,
        CatalogDictionaryTermSource source)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return GeneralErrors.ValueIsInvalid(nameof(phrase));
        }

        if (kind == CatalogDictionaryTermKind.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(targetValue))
        {
            return GeneralErrors.ValueIsInvalid(nameof(targetValue));
        }

        if (kind == CatalogDictionaryTermKind.Characteristic
            && string.IsNullOrWhiteSpace(targetCode))
        {
            return GeneralErrors.ValueIsInvalid(nameof(targetCode));
        }

        if (status == CatalogDictionaryTermStatus.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(status));
        }

        if (source == CatalogDictionaryTermSource.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(source));
        }

        if (priority <= 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(priority));
        }

        return new CatalogDictionaryTerm(
            Guid.CreateVersion7(),
            phrase.Trim(),
            NormalizeText(phrase),
            kind,
            NormalizeNullableText(targetCode),
            NormalizeText(targetValue),
            priority,
            status,
            source);
    }

    public UnitResult<DomainError> Approve()
    {
        if (Status == CatalogDictionaryTermStatus.Approved)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status == CatalogDictionaryTermStatus.Rejected)
        {
            return GeneralErrors.ValueIsInvalid(nameof(Status));
        }

        Status = CatalogDictionaryTermStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reject()
    {
        if (Status == CatalogDictionaryTermStatus.Approved)
        {
            return GeneralErrors.ValueIsInvalid(nameof(Status));
        }

        Status = CatalogDictionaryTermStatus.Rejected;

        return UnitResult.Success<DomainError>();
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private static string? NormalizeNullableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeText(value);
    }
}