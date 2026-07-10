using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public sealed class CatalogAssistantDictionarySuggestion : AggregateRoot
{
    private CatalogAssistantDictionarySuggestion(
        Guid id,
        string originalMessage,
        string unknownPhrase,
        string normalizedUnknownPhrase,
        CatalogDictionaryTermKind suggestedKind,
        string? suggestedTargetCode,
        string suggestedTargetValue,
        decimal confidence,
        Guid createdByUserId)
        : base(id)
    {
        OriginalMessage = originalMessage;
        UnknownPhrase = unknownPhrase;
        NormalizedUnknownPhrase = normalizedUnknownPhrase;
        SuggestedKind = suggestedKind;
        SuggestedTargetCode = suggestedTargetCode;
        SuggestedTargetValue = suggestedTargetValue;
        Confidence = confidence;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        Status = CatalogAssistantDictionarySuggestionStatus.Pending;
    }

    private CatalogAssistantDictionarySuggestion()
    {
    }

    public string OriginalMessage { get; private set; } = null!;

    public string UnknownPhrase { get; private set; } = null!;

    public string NormalizedUnknownPhrase { get; private set; } = null!;

    public CatalogDictionaryTermKind SuggestedKind { get; private set; }

    public string? SuggestedTargetCode { get; private set; }

    public string SuggestedTargetValue { get; private set; } = null!;

    public decimal Confidence { get; private set; }

    public CatalogAssistantDictionarySuggestionStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string? ReviewComment { get; private set; }

    public bool IsPending => Status == CatalogAssistantDictionarySuggestionStatus.Pending;

    public bool IsApproved => Status == CatalogAssistantDictionarySuggestionStatus.Approved;

    public bool IsRejected => Status == CatalogAssistantDictionarySuggestionStatus.Rejected;

    public static Result<CatalogAssistantDictionarySuggestion, DomainError> Create(
        string originalMessage,
        string unknownPhrase,
        CatalogDictionaryTermKind suggestedKind,
        string? suggestedTargetCode,
        string suggestedTargetValue,
        decimal confidence,
        Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(originalMessage))
        {
            return GeneralErrors.ValueIsInvalid(nameof(originalMessage));
        }

        if (string.IsNullOrWhiteSpace(unknownPhrase))
        {
            return GeneralErrors.ValueIsInvalid(nameof(unknownPhrase));
        }

        if (suggestedKind == CatalogDictionaryTermKind.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedKind));
        }

        if (suggestedKind == CatalogDictionaryTermKind.Characteristic
            && string.IsNullOrWhiteSpace(suggestedTargetCode))
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedTargetCode));
        }

        if (string.IsNullOrWhiteSpace(suggestedTargetValue))
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedTargetValue));
        }

        if (confidence < 0 || confidence > 1)
        {
            return GeneralErrors.ValueIsInvalid(nameof(confidence));
        }

        if (createdByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(createdByUserId));
        }

        return new CatalogAssistantDictionarySuggestion(
            Guid.CreateVersion7(),
            originalMessage.Trim(),
            unknownPhrase.Trim(),
            NormalizeText(unknownPhrase),
            suggestedKind,
            NormalizeNullableText(suggestedTargetCode),
            NormalizeText(suggestedTargetValue),
            confidence,
            createdByUserId);
    }

    public UnitResult<DomainError> Approve(
        Guid reviewedByUserId,
        string? reviewComment)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        Status = CatalogAssistantDictionarySuggestionStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewComment = NormalizeComment(reviewComment);

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reject(
        Guid reviewedByUserId,
        string? reviewComment)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        Status = CatalogAssistantDictionarySuggestionStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewComment = NormalizeComment(reviewComment);

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

    private static string? NormalizeComment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}