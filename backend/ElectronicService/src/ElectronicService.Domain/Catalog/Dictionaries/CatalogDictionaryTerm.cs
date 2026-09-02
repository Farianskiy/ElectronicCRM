using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public sealed class CatalogDictionaryTerm : AggregateRoot
{
    public const int DisableReasonMaxLength = 500;

    private CatalogDictionaryTerm(
        Guid id,
        Guid? productTypeId,
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
        ProductTypeId = productTypeId;
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
        DisabledAtUtc = null;
        DisabledByUserId = null;
        DisableReason = null;
        ReactivatedAtUtc = null;
        ReactivatedByUserId = null;
    }

    private CatalogDictionaryTerm()
    {
    }

    public Guid? ProductTypeId { get; private set; }

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

    public DateTime? DisabledAtUtc { get; private set; }

    public Guid? DisabledByUserId { get; private set; }

    public string? DisableReason { get; private set; }

    public DateTime? ReactivatedAtUtc { get; private set; }

    public Guid? ReactivatedByUserId { get; private set; }

    public static Result<CatalogDictionaryTerm, DomainError> Create(
        string phrase,
        CatalogDictionaryTermKind kind,
        string? targetCode,
        string targetValue,
        int priority,
        CatalogDictionaryTermStatus status,
        CatalogDictionaryTermSource source,
        Guid? productTypeId)
    {
        if (productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

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

        if (kind == CatalogDictionaryTermKind.Characteristic && string.IsNullOrWhiteSpace(targetCode))
        {
            return GeneralErrors.ValueIsInvalid(nameof(targetCode));
        }

        if (status == CatalogDictionaryTermStatus.None || status == CatalogDictionaryTermStatus.Disabled || !Enum.IsDefined(status))
        {
            return CatalogDictionaryTermErrors.StatusIsInvalid(status);
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
            productTypeId,
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

        if (Status != CatalogDictionaryTermStatus.Pending)
        {
            return UnitResult.Failure(CatalogDictionaryTermErrors.InvalidStatusTransition(Id, Status, CatalogDictionaryTermStatus.Approved));
        }

        Status = CatalogDictionaryTermStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reject()
    {
        if (Status == CatalogDictionaryTermStatus.Rejected)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status != CatalogDictionaryTermStatus.Pending)
        {
            return UnitResult.Failure(CatalogDictionaryTermErrors.InvalidStatusTransition(Id, Status, CatalogDictionaryTermStatus.Rejected));
        }

        Status = CatalogDictionaryTermStatus.Rejected;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Disable(string reason, Guid disabledByUserId)
    {
        if (disabledByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(disabledByUserId)));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsRequired(nameof(reason)));
        }

        var trimmedReason = reason.Trim();

        if (trimmedReason.Length > DisableReasonMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(reason), DisableReasonMaxLength));
        }

        if (Status == CatalogDictionaryTermStatus.Disabled)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status != CatalogDictionaryTermStatus.Approved)
        {
            return UnitResult.Failure(CatalogDictionaryTermErrors.InvalidStatusTransition(Id, Status, CatalogDictionaryTermStatus.Disabled));
        }

        var disabledAtUtc = DateTime.UtcNow;

        Status = CatalogDictionaryTermStatus.Disabled;
        DisabledAtUtc = disabledAtUtc;
        DisabledByUserId = disabledByUserId;
        DisableReason = trimmedReason;
        ReactivatedAtUtc = null;
        ReactivatedByUserId = null;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reactivate(Guid reactivatedByUserId)
    {
        if (reactivatedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reactivatedByUserId)));
        }

        if (Status == CatalogDictionaryTermStatus.Approved)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status != CatalogDictionaryTermStatus.Disabled)
        {
            return UnitResult.Failure(CatalogDictionaryTermErrors.InvalidStatusTransition(Id, Status, CatalogDictionaryTermStatus.Approved));
        }

        Status = CatalogDictionaryTermStatus.Approved;
        ReactivatedAtUtc = DateTime.UtcNow;
        ReactivatedByUserId = reactivatedByUserId;

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