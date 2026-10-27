using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionFeedback : AggregateRoot
{
    public const int ProductNameMaxLength = 2000;

    public const int ProductTypeCodeMaxLength = 200;

    public const int CharacteristicCodeMaxLength = 200;

    public const int CharacteristicValueMaxLength = 2000;

    public const int SuggestedSourceMaxLength = 100;

    public const int ModelVersionMaxLength = 200;

    public const int ReviewerRoleMaxLength = 100;

    public const int EvidenceSnapshotMaxLength = 32000;
    public const int AnalogyStaleReasonMaxLength = 1000;

    private CatalogRecognitionFeedback(
        Guid id,
        string productName,
        string normalizedProductName,
        Guid? manufacturerId,
        Guid productTypeId,
        string productTypeCodeSnapshot,
        Guid characteristicDefinitionId,
        string characteristicCodeSnapshot,
        string? suggestedRawValue,
        string? suggestedNormalizedValue,
        decimal? suggestedConfidence,
        string? suggestedSource,
        int? spanStart,
        int? spanLength,
        Guid? dictionaryTermId,
        Guid? recognitionProfileId,
        string? modelVersion,
        Guid? importBatchId,
        Guid? importRowId,
        CatalogRecognitionFeedbackStatus status,
        CatalogRecognitionFeedbackType feedbackType,
        CatalogRecognitionLabelQuality labelQuality,
        string? finalNormalizedValue,
        Guid? reviewedByUserId,
        string? reviewerRole,
        DateTime createdAtUtc,
        DateTime? finalizedAtUtc,
        bool isTrainingEligible,
        string? evidenceSnapshotJson)
        : base(id)
    {
        ProductName = productName;
        NormalizedProductName = normalizedProductName;
        ManufacturerId = manufacturerId;
        ProductTypeId = productTypeId;
        ProductTypeCodeSnapshot = productTypeCodeSnapshot;
        CharacteristicDefinitionId = characteristicDefinitionId;
        CharacteristicCodeSnapshot = characteristicCodeSnapshot;
        SuggestedRawValue = suggestedRawValue;
        SuggestedNormalizedValue = suggestedNormalizedValue;
        SuggestedConfidence = suggestedConfidence;
        SuggestedSource = suggestedSource;
        SpanStart = spanStart;
        SpanLength = spanLength;
        DictionaryTermId = dictionaryTermId;
        RecognitionProfileId = recognitionProfileId;
        ModelVersion = modelVersion;
        ImportBatchId = importBatchId;
        ImportRowId = importRowId;
        Status = status;
        FeedbackType = feedbackType;
        LabelQuality = labelQuality;
        FinalNormalizedValue = finalNormalizedValue;
        ReviewedByUserId = reviewedByUserId;
        ReviewerRole = reviewerRole;
        CreatedAtUtc = createdAtUtc;
        FinalizedAtUtc = finalizedAtUtc;
        IsTrainingEligible = isTrainingEligible;
        EvidenceSnapshotJson = evidenceSnapshotJson;
    }

    private CatalogRecognitionFeedback()
    {
    }

    public string ProductName { get; private set; } = string.Empty;

    public string NormalizedProductName { get; private set; } = string.Empty;

    public Guid? ManufacturerId { get; private set; }

    public Guid ProductTypeId { get; private set; }

    public string ProductTypeCodeSnapshot { get; private set; } = string.Empty;

    public Guid CharacteristicDefinitionId { get; private set; }

    public string CharacteristicCodeSnapshot { get; private set; } = string.Empty;

    public string? SuggestedRawValue { get; private set; }

    public string? SuggestedNormalizedValue { get; private set; }

    public decimal? SuggestedConfidence { get; private set; }

    public string? SuggestedSource { get; private set; }

    public int? SpanStart { get; private set; }

    public int? SpanLength { get; private set; }

    public string? FinalNormalizedValue { get; private set; }

    public string? ConfirmedRawValue { get; private set; }

    public int? ConfirmedSpanStart { get; private set; }

    public int? ConfirmedSpanLength { get; private set; }

    public CatalogRecognitionFeedbackStatus Status { get; private set; }

    public CatalogRecognitionFeedbackType FeedbackType { get; private set; }

    public CatalogRecognitionLabelQuality LabelQuality { get; private set; }

    public Guid? DictionaryTermId { get; private set; }

    public Guid? RecognitionProfileId { get; private set; }

    public string? ModelVersion { get; private set; }

    public Guid? ImportBatchId { get; private set; }

    public Guid? ImportRowId { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public string? ReviewerRole { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? FinalizedAtUtc { get; private set; }

    public bool IsTrainingEligible { get; private set; }

    public string? EvidenceSnapshotJson { get; private set; }
    public int AnalogyDisplayCount { get; private set; }
    public int AnalogyStaleCount { get; private set; }
    public DateTime? AnalogyLastShownAtUtc { get; private set; }
    public DateTime? AnalogyDecisionRecordedAtUtc { get; private set; }
    public string? AnalogyLastStaleReason { get; private set; }
    public DateTime? ExcludedAtUtc { get; private set; }
    public Guid? ExcludedByUserId { get; private set; }
    public string? ExclusionReason { get; private set; }

    public UnitResult<DomainError> ExcludeFromLearning(Guid userId, string reason)
    {
        if (ExcludedAtUtc.HasValue) return UnitResult.Success<DomainError>();
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reason)));
        ExcludedAtUtc = DateTime.UtcNow;
        ExcludedByUserId = userId;
        ExclusionReason = reason.Trim();
        return UnitResult.Success<DomainError>();
    }

    public bool IsPending => Status == CatalogRecognitionFeedbackStatus.Pending;

    public bool IsFinalized => Status == CatalogRecognitionFeedbackStatus.Finalized;

    public static Result<CatalogRecognitionFeedback, DomainError> Create(
        string productName,
        string normalizedProductName,
        Guid manufacturerId,
        Guid productTypeId,
        string productTypeCodeSnapshot,
        Guid characteristicDefinitionId,
        string characteristicCodeSnapshot,
        string? suggestedRawValue,
        string? suggestedNormalizedValue,
        decimal? suggestedConfidence,
        string? suggestedSource,
        int? spanStart,
        int? spanLength,
        CatalogRecognitionFeedbackType feedbackType,
        string? finalNormalizedValue,
        Guid? dictionaryTermId,
        Guid? recognitionProfileId,
        string? modelVersion,
        Guid? importBatchId,
        Guid? importRowId,
        string? evidenceSnapshotJson = null)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return GeneralErrors.ValueIsRequired(nameof(productName));
        }

        if (string.IsNullOrWhiteSpace(normalizedProductName))
        {
            return GeneralErrors.ValueIsRequired(nameof(normalizedProductName));
        }

        if (manufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (string.IsNullOrWhiteSpace(productTypeCodeSnapshot))
        {
            return GeneralErrors.ValueIsRequired(nameof(productTypeCodeSnapshot));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (string.IsNullOrWhiteSpace(characteristicCodeSnapshot))
        {
            return GeneralErrors.ValueIsRequired(nameof(characteristicCodeSnapshot));
        }

        var trimmedProductName = productName.Trim();
        var trimmedNormalizedProductName = normalizedProductName.Trim();
        var trimmedProductTypeCodeSnapshot = productTypeCodeSnapshot.Trim();
        var trimmedCharacteristicCodeSnapshot = characteristicCodeSnapshot.Trim();
        var trimmedSuggestedRawValue = NormalizeOptionalValue(suggestedRawValue);
        var trimmedSuggestedNormalizedValue = NormalizeOptionalValue(suggestedNormalizedValue);
        var trimmedSuggestedSource = NormalizeOptionalValue(suggestedSource);
        var trimmedModelVersion = NormalizeOptionalValue(modelVersion);
        var trimmedEvidenceSnapshotJson = NormalizeOptionalValue(
            evidenceSnapshotJson);

        if (trimmedProductName.Length > ProductNameMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(productName), ProductNameMaxLength);
        }

        if (trimmedNormalizedProductName.Length > ProductNameMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(normalizedProductName), ProductNameMaxLength);
        }

        if (trimmedProductTypeCodeSnapshot.Length > ProductTypeCodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(productTypeCodeSnapshot), ProductTypeCodeMaxLength);
        }

        if (trimmedCharacteristicCodeSnapshot.Length > CharacteristicCodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(characteristicCodeSnapshot), CharacteristicCodeMaxLength);
        }

        if (trimmedSuggestedRawValue is not null && trimmedSuggestedRawValue.Length > CharacteristicValueMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(suggestedRawValue), CharacteristicValueMaxLength);
        }

        if (trimmedSuggestedNormalizedValue is not null && trimmedSuggestedNormalizedValue.Length > CharacteristicValueMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(suggestedNormalizedValue), CharacteristicValueMaxLength);
        }

        if (trimmedSuggestedSource is not null && trimmedSuggestedSource.Length > SuggestedSourceMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(suggestedSource), SuggestedSourceMaxLength);
        }

        if (trimmedModelVersion is not null && trimmedModelVersion.Length > ModelVersionMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(modelVersion), ModelVersionMaxLength);
        }

        if (trimmedEvidenceSnapshotJson is not null
            && trimmedEvidenceSnapshotJson.Length > EvidenceSnapshotMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(evidenceSnapshotJson),
                EvidenceSnapshotMaxLength);
        }

        if (suggestedConfidence.HasValue && (suggestedConfidence.Value < 0m || suggestedConfidence.Value > 1m))
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedConfidence));
        }

        var hasSuggestedRawValue = trimmedSuggestedRawValue is not null;
        var hasSuggestedNormalizedValue = trimmedSuggestedNormalizedValue is not null;
        var hasSuggestedSource = trimmedSuggestedSource is not null;

        if (hasSuggestedRawValue != hasSuggestedNormalizedValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedNormalizedValue));
        }

        if ((hasSuggestedRawValue || hasSuggestedNormalizedValue) != hasSuggestedSource)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedSource));
        }

        if (suggestedConfidence.HasValue && !hasSuggestedNormalizedValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suggestedConfidence));
        }

        if (spanStart.HasValue != spanLength.HasValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(spanLength));
        }

        if (spanStart.HasValue && spanStart.Value < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(spanStart));
        }

        if (spanLength.HasValue && spanLength.Value <= 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(spanLength));
        }

        if (spanStart.HasValue && spanLength.HasValue && (long)spanStart.Value + spanLength.Value > trimmedProductName.Length)
        {
            return GeneralErrors.ValueIsInvalid(nameof(spanLength));
        }

        if ((spanStart.HasValue || spanLength.HasValue) && !hasSuggestedRawValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(spanStart));
        }

        if (dictionaryTermId.HasValue && dictionaryTermId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(dictionaryTermId));
        }

        if (recognitionProfileId.HasValue && recognitionProfileId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(recognitionProfileId));
        }

        if (importBatchId.HasValue && importBatchId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(importBatchId));
        }

        if (importRowId.HasValue && importRowId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(importRowId));
        }

        if (importBatchId.HasValue != importRowId.HasValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(importRowId));
        }

        var decisionResult = ValidateDecision(
            feedbackType,
            finalNormalizedValue,
            trimmedSuggestedNormalizedValue);

        if (decisionResult.IsFailure)
        {
            return Result.Failure<CatalogRecognitionFeedback, DomainError>(
                decisionResult.Error);
        }

        var feedback = new CatalogRecognitionFeedback(
            Guid.CreateVersion7(),
            trimmedProductName,
            trimmedNormalizedProductName,
            manufacturerId,
            productTypeId,
            trimmedProductTypeCodeSnapshot,
            characteristicDefinitionId,
            trimmedCharacteristicCodeSnapshot,
            trimmedSuggestedRawValue,
            trimmedSuggestedNormalizedValue,
            suggestedConfidence,
            trimmedSuggestedSource,
            spanStart,
            spanLength,
            dictionaryTermId,
            recognitionProfileId,
            trimmedModelVersion,
            importBatchId,
            importRowId,
            CatalogRecognitionFeedbackStatus.Pending,
            feedbackType,
            CatalogRecognitionLabelQuality.None,
            decisionResult.Value,
            reviewedByUserId: null,
            reviewerRole: null,
            DateTime.UtcNow,
            finalizedAtUtc: null,
            isTrainingEligible: false,
            trimmedEvidenceSnapshotJson);

        if (feedbackType == CatalogRecognitionFeedbackType.Shown)
        {
            feedback.AnalogyDisplayCount = 1;
            feedback.AnalogyLastShownAtUtc = feedback.CreatedAtUtc;
        }

        return feedback;
    }

    public bool MatchesAnalogyObservation(
        string normalizedProductName,
        string suggestedNormalizedValue,
        string modelVersion,
        string evidenceSnapshotJson)
    {
        return FeedbackType == CatalogRecognitionFeedbackType.Shown
            && string.Equals(
                NormalizedProductName,
                normalizedProductName,
                StringComparison.Ordinal)
            && string.Equals(
                SuggestedNormalizedValue,
                suggestedNormalizedValue,
                StringComparison.Ordinal)
            && string.Equals(ModelVersion, modelVersion, StringComparison.Ordinal)
            && string.Equals(
                EvidenceSnapshotJson,
                evidenceSnapshotJson,
                StringComparison.Ordinal);
    }

    public UnitResult<DomainError> RefreshAnalogyObservation(
        string productName,
        string normalizedProductName,
        Guid manufacturerId,
        Guid productTypeId,
        string productTypeCodeSnapshot,
        string suggestedRawValue,
        string suggestedNormalizedValue,
        decimal suggestedConfidence,
        string suggestedSource,
        string modelVersion,
        string evidenceSnapshotJson,
        string staleReason)
    {
        if (!IsPending || IsFinalized)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        var normalizedStaleReason = NormalizeOptionalValue(staleReason);
        if (normalizedStaleReason is null
            || normalizedStaleReason.Length > AnalogyStaleReasonMaxLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(staleReason)));
        }

        var replacement = Create(
            productName,
            normalizedProductName,
            manufacturerId,
            productTypeId,
            productTypeCodeSnapshot,
            CharacteristicDefinitionId,
            CharacteristicCodeSnapshot,
            suggestedRawValue,
            suggestedNormalizedValue,
            suggestedConfidence,
            suggestedSource,
            spanStart: null,
            spanLength: null,
            CatalogRecognitionFeedbackType.Shown,
            finalNormalizedValue: null,
            dictionaryTermId: null,
            recognitionProfileId: null,
            modelVersion,
            ImportBatchId,
            ImportRowId,
            evidenceSnapshotJson);

        if (replacement.IsFailure)
        {
            return UnitResult.Failure(replacement.Error);
        }

        var value = replacement.Value;
        ProductName = value.ProductName;
        NormalizedProductName = value.NormalizedProductName;
        ManufacturerId = value.ManufacturerId;
        ProductTypeId = value.ProductTypeId;
        ProductTypeCodeSnapshot = value.ProductTypeCodeSnapshot;
        SuggestedRawValue = value.SuggestedRawValue;
        SuggestedNormalizedValue = value.SuggestedNormalizedValue;
        SuggestedConfidence = value.SuggestedConfidence;
        SuggestedSource = value.SuggestedSource;
        ModelVersion = value.ModelVersion;
        EvidenceSnapshotJson = value.EvidenceSnapshotJson;
        FeedbackType = CatalogRecognitionFeedbackType.Shown;
        FinalNormalizedValue = null;
        AnalogyDisplayCount++;
        AnalogyStaleCount++;
        AnalogyLastShownAtUtc = DateTime.UtcNow;
        AnalogyDecisionRecordedAtUtc = null;
        AnalogyLastStaleReason = normalizedStaleReason;
        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> RecordAnalogyDecision(
        CatalogRecognitionFeedbackType feedbackType,
        string? finalNormalizedValue,
        string evidenceSnapshotJson)
    {
        if (FeedbackType != CatalogRecognitionFeedbackType.Shown
            || AnalogyDisplayCount <= 0)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(FeedbackType)));
        }

        if (string.IsNullOrWhiteSpace(evidenceSnapshotJson)
            || evidenceSnapshotJson.Trim().Length > EvidenceSnapshotMaxLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(evidenceSnapshotJson)));
        }

        var update = UpdatePendingDecision(feedbackType, finalNormalizedValue);
        if (update.IsFailure)
        {
            return update;
        }

        EvidenceSnapshotJson = evidenceSnapshotJson.Trim();
        AnalogyDecisionRecordedAtUtc = DateTime.UtcNow;
        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> SetConfirmedSpan(int start, int length)
    {
        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        if (FeedbackType == CatalogRecognitionFeedbackType.Rejected || string.IsNullOrWhiteSpace(FinalNormalizedValue))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(FinalNormalizedValue)));
        }

        if (start < 0 || start >= ProductName.Length)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(start)));
        }

        if (length <= 0 || length > ProductName.Length - start)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(length)));
        }

        var end = start + length;

        if ((start > 0 && char.IsLowSurrogate(ProductName[start]) && char.IsHighSurrogate(ProductName[start - 1])) || (end < ProductName.Length && char.IsLowSurrogate(ProductName[end]) && char.IsHighSurrogate(ProductName[end - 1])))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(length)));
        }

        var rawValue = ProductName.Substring(start, length);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(length)));
        }

        ConfirmedRawValue = rawValue;
        ConfirmedSpanStart = start;
        ConfirmedSpanLength = length;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> ClearConfirmedSpan()
    {
        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        ConfirmedRawValue = null;
        ConfirmedSpanStart = null;
        ConfirmedSpanLength = null;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> UpdatePendingDecision(CatalogRecognitionFeedbackType feedbackType, string? finalNormalizedValue)
    {
        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        var decisionResult = ValidateDecision(
            feedbackType,
            finalNormalizedValue,
            SuggestedNormalizedValue);

        if (decisionResult.IsFailure)
        {
            return UnitResult.Failure(decisionResult.Error);
        }

        var decisionChanged = FeedbackType != feedbackType || !string.Equals(FinalNormalizedValue, decisionResult.Value, StringComparison.Ordinal);

        if (decisionChanged)
        {
            ConfirmedRawValue = null;
            ConfirmedSpanStart = null;
            ConfirmedSpanLength = null;
        }

        FeedbackType = feedbackType;
        FinalNormalizedValue = decisionResult.Value;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Finalize(
        CatalogRecognitionLabelQuality labelQuality,
        Guid reviewedByUserId,
        string reviewerRole,
        bool isTrainingEligible)
    {
        if (!IsPending)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        if (FeedbackType == CatalogRecognitionFeedbackType.None || !Enum.IsDefined(FeedbackType))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(FeedbackType)));
        }

        if (labelQuality == CatalogRecognitionLabelQuality.None || !Enum.IsDefined(labelQuality))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(labelQuality)));
        }

        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(reviewedByUserId)));
        }

        if (string.IsNullOrWhiteSpace(reviewerRole))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsRequired(nameof(reviewerRole)));
        }

        var trimmedReviewerRole = reviewerRole.Trim();

        if (trimmedReviewerRole.Length > ReviewerRoleMaxLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsTooLong(
                    nameof(reviewerRole),
                    ReviewerRoleMaxLength));
        }

        if (isTrainingEligible && labelQuality == CatalogRecognitionLabelQuality.Weak)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(isTrainingEligible)));
        }

        Status = CatalogRecognitionFeedbackStatus.Finalized;
        LabelQuality = labelQuality;
        ReviewedByUserId = reviewedByUserId;
        ReviewerRole = trimmedReviewerRole;
        FinalizedAtUtc = DateTime.UtcNow;
        IsTrainingEligible = isTrainingEligible;

        return UnitResult.Success<DomainError>();
    }

    private static Result<string?, DomainError> ValidateDecision(
        CatalogRecognitionFeedbackType feedbackType,
        string? finalNormalizedValue,
        string? suggestedNormalizedValue)
    {
        if (feedbackType == CatalogRecognitionFeedbackType.None || !Enum.IsDefined(feedbackType))
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(feedbackType)));
        }

        var trimmedFinalNormalizedValue = NormalizeOptionalValue(finalNormalizedValue);

        if (trimmedFinalNormalizedValue is not null && trimmedFinalNormalizedValue.Length > CharacteristicValueMaxLength)
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsTooLong(
                    nameof(finalNormalizedValue),
                    CharacteristicValueMaxLength));
        }

        if ((feedbackType == CatalogRecognitionFeedbackType.Rejected
                || feedbackType == CatalogRecognitionFeedbackType.Shown)
            && trimmedFinalNormalizedValue is not null)
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(finalNormalizedValue)));
        }

        if (feedbackType != CatalogRecognitionFeedbackType.Rejected
            && feedbackType != CatalogRecognitionFeedbackType.Shown
            && trimmedFinalNormalizedValue is null)
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsRequired(nameof(finalNormalizedValue)));
        }

        if (feedbackType == CatalogRecognitionFeedbackType.Accepted && suggestedNormalizedValue is null)
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsRequired(nameof(suggestedNormalizedValue)));
        }

        if (feedbackType == CatalogRecognitionFeedbackType.Accepted && !string.Equals(trimmedFinalNormalizedValue, suggestedNormalizedValue, StringComparison.Ordinal))
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(finalNormalizedValue)));
        }

        if (feedbackType == CatalogRecognitionFeedbackType.Corrected && suggestedNormalizedValue is null)
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsRequired(nameof(suggestedNormalizedValue)));
        }

        if (feedbackType == CatalogRecognitionFeedbackType.Corrected && string.Equals(trimmedFinalNormalizedValue, suggestedNormalizedValue, StringComparison.Ordinal))
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(feedbackType)));
        }

        if (feedbackType == CatalogRecognitionFeedbackType.AddedManually && suggestedNormalizedValue is not null)
        {
            return Result.Failure<string?, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(feedbackType)));
        }

        return Result.Success<string?, DomainError>(trimmedFinalNormalizedValue);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
