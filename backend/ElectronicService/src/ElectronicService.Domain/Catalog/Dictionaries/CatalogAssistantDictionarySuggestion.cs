using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public sealed class CatalogAssistantDictionarySuggestion : AggregateRoot
{
    public const int OriginalMessageMaxLength = 1000;

    public const int UnknownPhraseMaxLength = 300;

    public const int SuggestedTargetCodeMaxLength = 100;

    public const int SuggestedTargetValueMaxLength = 300;

    public const int ReviewCommentMaxLength = 1000;

    private CatalogAssistantDictionarySuggestion(
        Guid id,
        string originalMessage,
        string unknownPhrase,
        string normalizedUnknownPhrase,
        CatalogDictionaryTermKind suggestedKind,
        string? suggestedTargetCode,
        string suggestedTargetValue,
        decimal confidence,
        CatalogDictionarySuggestionSource source,
        Guid? manufacturerId,
        Guid? productTypeId,
        Guid? characteristicDefinitionId,
        int occurrenceCount,
        int acceptedEvidenceCount,
        int correctedEvidenceCount,
        int rejectedEvidenceCount,
        bool generatedAutomatically,
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
        Source = source;
        ManufacturerId = manufacturerId;
        ProductTypeId = productTypeId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        OccurrenceCount = occurrenceCount;
        AcceptedEvidenceCount = acceptedEvidenceCount;
        CorrectedEvidenceCount = correctedEvidenceCount;
        RejectedEvidenceCount = rejectedEvidenceCount;
        GeneratedAutomatically = generatedAutomatically;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        Status = CatalogAssistantDictionarySuggestionStatus.Pending;
    }

    private CatalogAssistantDictionarySuggestion()
    {
    }

    public string OriginalMessage { get; private set; } = string.Empty;

    public string UnknownPhrase { get; private set; } = string.Empty;

    public string NormalizedUnknownPhrase { get; private set; } = string.Empty;

    public CatalogDictionaryTermKind SuggestedKind { get; private set; }

    public string? SuggestedTargetCode { get; private set; }

    public string SuggestedTargetValue { get; private set; } = string.Empty;

    public decimal Confidence { get; private set; }

    public CatalogDictionarySuggestionSource Source { get; private set; }

    public Guid? ManufacturerId { get; private set; }

    public Guid? ProductTypeId { get; private set; }

    public Guid? CharacteristicDefinitionId { get; private set; }

    public int OccurrenceCount { get; private set; }

    public int AcceptedEvidenceCount { get; private set; }

    public int CorrectedEvidenceCount { get; private set; }

    public int RejectedEvidenceCount { get; private set; }

    public bool GeneratedAutomatically { get; private set; }

    public string? ApprovedPhrase { get; private set; }

    public CatalogDictionaryTermKind? ApprovedKind { get; private set; }

    public string? ApprovedTargetCode { get; private set; }

    public string? ApprovedTargetValue { get; private set; }

    public Guid? ApprovedManufacturerId { get; private set; }

    public Guid? ApprovedProductTypeId { get; private set; }

    public Guid? ApprovedCharacteristicDefinitionId { get; private set; }

    public int? ApprovedPriority { get; private set; }

    public Guid? CreatedDictionaryTermId { get; private set; }

    public CatalogAssistantDictionarySuggestionStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string? ReviewComment { get; private set; }

    public bool IsPending => Status == CatalogAssistantDictionarySuggestionStatus.Pending;

    public bool IsApproved => Status == CatalogAssistantDictionarySuggestionStatus.Approved;

    public bool IsRejected => Status == CatalogAssistantDictionarySuggestionStatus.Rejected;

    public bool IsScopedToProductType => ProductTypeId.HasValue;

    public bool IsGeneratedFromRecognitionLearning => Source == CatalogDictionarySuggestionSource.RecognitionLearning;

    public static Result<CatalogAssistantDictionarySuggestion, DomainError> Create(
        string originalMessage,
        string unknownPhrase,
        CatalogDictionaryTermKind suggestedKind,
        string? suggestedTargetCode,
        string suggestedTargetValue,
        decimal confidence,
        Guid createdByUserId)
    {
        return CreateInternal(
            originalMessage,
            unknownPhrase,
            NormalizeText(unknownPhrase),
            suggestedKind,
            suggestedTargetCode,
            suggestedTargetValue,
            confidence,
            CatalogDictionarySuggestionSource.Assistant,
            manufacturerId: null,
            productTypeId: null,
            characteristicDefinitionId: null,
            occurrenceCount: 1,
            acceptedEvidenceCount: 0,
            correctedEvidenceCount: 0,
            rejectedEvidenceCount: 0,
            generatedAutomatically: false,
            createdByUserId);
    }

    public static Result<CatalogAssistantDictionarySuggestion, DomainError> CreateFromRecognitionCandidate(
        CatalogRecognitionCandidate candidate,
        decimal confidence,
        Guid createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!candidate.IsAccumulating || candidate.HasSuggestion)
        {
            return GeneralErrors.ValueIsInvalid(nameof(candidate.Status));
        }

        if (candidate.CorrectedCount <= 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(candidate.CorrectedCount));
        }

        if (!candidate.ManufacturerId.HasValue || candidate.ManufacturerId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(candidate.ManufacturerId));
        }

        var originalMessage = $"Автоматическое предложение на основе Recognition Candidate {candidate.Id}. Тип товара: {candidate.ProductTypeCodeSnapshot}. Характеристика: {candidate.CharacteristicCodeSnapshot}. Всего наблюдений: {candidate.OccurrenceCount}. Подтверждений: {candidate.AcceptedCount}. Исправлений: {candidate.CorrectedCount}. Отклонений: {candidate.RejectedCount}.";

        return CreateInternal(
            originalMessage,
            candidate.Phrase,
            candidate.NormalizedPhrase,
            CatalogDictionaryTermKind.Characteristic,
            candidate.CharacteristicCodeSnapshot,
            candidate.ProposedValue,
            confidence,
            CatalogDictionarySuggestionSource.RecognitionLearning,
            candidate.ManufacturerId,
            candidate.ProductTypeId,
            candidate.CharacteristicDefinitionId,
            candidate.OccurrenceCount,
            candidate.AcceptedCount,
            candidate.CorrectedCount,
            candidate.RejectedCount,
            generatedAutomatically: true,
            createdByUserId);
    }

    public UnitResult<DomainError> ApproveWithDecision(
    CatalogDictionarySuggestionApprovalDecision decision,
    Guid createdDictionaryTermId,
    Guid reviewedByUserId,
    string? reviewComment)
    {
        ArgumentNullException.ThrowIfNull(decision);

        if (createdDictionaryTermId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(createdDictionaryTermId)));
        }

        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        var normalizedReviewComment = NormalizeComment(reviewComment);

        if (normalizedReviewComment is not null && normalizedReviewComment.Length > ReviewCommentMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(reviewComment), ReviewCommentMaxLength));
        }

        ApprovedPhrase = decision.Phrase;
        ApprovedKind = decision.Kind;
        ApprovedTargetCode = decision.TargetCode;
        ApprovedTargetValue = decision.TargetValue;
        ApprovedManufacturerId = decision.ManufacturerId;
        ApprovedProductTypeId = decision.ProductTypeId;
        ApprovedCharacteristicDefinitionId = decision.CharacteristicDefinitionId;
        ApprovedPriority = decision.Priority;
        CreatedDictionaryTermId = createdDictionaryTermId;
        Status = CatalogAssistantDictionarySuggestionStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewComment = normalizedReviewComment;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Approve(Guid reviewedByUserId, string? reviewComment)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        var normalizedReviewComment = NormalizeComment(reviewComment);

        if (normalizedReviewComment is not null && normalizedReviewComment.Length > ReviewCommentMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(reviewComment), ReviewCommentMaxLength));
        }

        Status = CatalogAssistantDictionarySuggestionStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewComment = normalizedReviewComment;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reject(Guid reviewedByUserId, string? reviewComment)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        var normalizedReviewComment = NormalizeComment(reviewComment);

        if (normalizedReviewComment is not null && normalizedReviewComment.Length > ReviewCommentMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(reviewComment), ReviewCommentMaxLength));
        }

        Status = CatalogAssistantDictionarySuggestionStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewComment = normalizedReviewComment;

        return UnitResult.Success<DomainError>();
    }

    private static Result<CatalogAssistantDictionarySuggestion, DomainError> CreateInternal(
        string originalMessage,
        string unknownPhrase,
        string normalizedUnknownPhrase,
        CatalogDictionaryTermKind suggestedKind,
        string? suggestedTargetCode,
        string suggestedTargetValue,
        decimal confidence,
        CatalogDictionarySuggestionSource source,
        Guid? manufacturerId,
        Guid? productTypeId,
        Guid? characteristicDefinitionId,
        int occurrenceCount,
        int acceptedEvidenceCount,
        int correctedEvidenceCount,
        int rejectedEvidenceCount,
        bool generatedAutomatically,
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

        if (string.IsNullOrWhiteSpace(normalizedUnknownPhrase))
        {
            return GeneralErrors.ValueIsInvalid(nameof(normalizedUnknownPhrase));
        }

        if (suggestedKind == CatalogDictionaryTermKind.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedKind));
        }

        if (suggestedKind == CatalogDictionaryTermKind.Characteristic && string.IsNullOrWhiteSpace(suggestedTargetCode))
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

        if (source == CatalogDictionarySuggestionSource.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(source));
        }

        if (manufacturerId.HasValue && manufacturerId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (productTypeId.HasValue && productTypeId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (characteristicDefinitionId.HasValue && characteristicDefinitionId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (characteristicDefinitionId.HasValue && !productTypeId.HasValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (occurrenceCount <= 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(occurrenceCount));
        }

        if (acceptedEvidenceCount < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(acceptedEvidenceCount));
        }

        if (correctedEvidenceCount < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(correctedEvidenceCount));
        }

        if (rejectedEvidenceCount < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(rejectedEvidenceCount));
        }

        var evidenceCount = (long)acceptedEvidenceCount + correctedEvidenceCount + rejectedEvidenceCount;

        if (evidenceCount > occurrenceCount)
        {
            return GeneralErrors.ValueIsInvalid(nameof(occurrenceCount));
        }

        if (generatedAutomatically && source == CatalogDictionarySuggestionSource.Assistant)
        {
            return GeneralErrors.ValueIsInvalid(nameof(source));
        }

        if (!generatedAutomatically && source == CatalogDictionarySuggestionSource.RecognitionLearning)
        {
            return GeneralErrors.ValueIsInvalid(nameof(generatedAutomatically));
        }

        if (source == CatalogDictionarySuggestionSource.RecognitionLearning && !manufacturerId.HasValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (source == CatalogDictionarySuggestionSource.RecognitionLearning && (!productTypeId.HasValue || !characteristicDefinitionId.HasValue))
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (createdByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(createdByUserId));
        }

        var normalizedOriginalMessage = originalMessage.Trim();
        var normalizedUnknownPhraseValue = normalizedUnknownPhrase.Trim();
        var normalizedSuggestedTargetCode = NormalizeNullableText(suggestedTargetCode);
        var normalizedSuggestedTargetValue = NormalizeText(suggestedTargetValue);
        var trimmedUnknownPhrase = unknownPhrase.Trim();

        if (normalizedOriginalMessage.Length > OriginalMessageMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(originalMessage), OriginalMessageMaxLength);
        }

        if (trimmedUnknownPhrase.Length > UnknownPhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(unknownPhrase), UnknownPhraseMaxLength);
        }

        if (normalizedUnknownPhraseValue.Length > UnknownPhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(normalizedUnknownPhrase), UnknownPhraseMaxLength);
        }

        if (normalizedSuggestedTargetCode is not null && normalizedSuggestedTargetCode.Length > SuggestedTargetCodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(suggestedTargetCode), SuggestedTargetCodeMaxLength);
        }

        if (normalizedSuggestedTargetValue.Length > SuggestedTargetValueMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(suggestedTargetValue), SuggestedTargetValueMaxLength);
        }

        return new CatalogAssistantDictionarySuggestion(
            Guid.CreateVersion7(),
            normalizedOriginalMessage,
            trimmedUnknownPhrase,
            normalizedUnknownPhraseValue,
            suggestedKind,
            normalizedSuggestedTargetCode,
            normalizedSuggestedTargetValue,
            confidence,
            source,
            manufacturerId,
            productTypeId,
            characteristicDefinitionId,
            occurrenceCount,
            acceptedEvidenceCount,
            correctedEvidenceCount,
            rejectedEvidenceCount,
            generatedAutomatically,
            createdByUserId);
    }

    private static string NormalizeText(string value)
    {
        return value.Trim().ToUpperInvariant().Replace("Ё", "Е", StringComparison.Ordinal);
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