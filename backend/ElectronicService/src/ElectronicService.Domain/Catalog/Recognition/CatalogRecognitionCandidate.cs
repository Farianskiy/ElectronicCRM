using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionCandidate : AggregateRoot
{
    public const int PhraseMaxLength = 2000;

    public const int ProductTypeCodeMaxLength = 200;

    public const int CharacteristicCodeMaxLength = 200;

    public const int ProposedValueMaxLength = 2000;

    public const int CandidateKeyLength = 64;

    private CatalogRecognitionCandidate(
        Guid id,
        string phrase,
        string normalizedPhrase,
        Guid productTypeId,
        string productTypeCodeSnapshot,
        Guid characteristicDefinitionId,
        string characteristicCodeSnapshot,
        string proposedValue,
        string candidateKey,
        int occurrenceCount,
        int acceptedCount,
        int correctedCount,
        int rejectedCount,
        int distinctProductCount,
        CatalogRecognitionCandidateStatus status,
        Guid? suggestionId,
        DateTime firstSeenAtUtc,
        DateTime lastSeenAtUtc)
        : base(id)
    {
        Phrase = phrase;
        NormalizedPhrase = normalizedPhrase;
        ProductTypeId = productTypeId;
        ProductTypeCodeSnapshot = productTypeCodeSnapshot;
        CharacteristicDefinitionId = characteristicDefinitionId;
        CharacteristicCodeSnapshot = characteristicCodeSnapshot;
        ProposedValue = proposedValue;
        CandidateKey = candidateKey;
        OccurrenceCount = occurrenceCount;
        AcceptedCount = acceptedCount;
        CorrectedCount = correctedCount;
        RejectedCount = rejectedCount;
        DistinctProductCount = distinctProductCount;
        Status = status;
        SuggestionId = suggestionId;
        FirstSeenAtUtc = firstSeenAtUtc;
        LastSeenAtUtc = lastSeenAtUtc;
    }

    private CatalogRecognitionCandidate()
    {
    }

    public string Phrase { get; private set; } = string.Empty;

    public string NormalizedPhrase { get; private set; } = string.Empty;

    public Guid ProductTypeId { get; private set; }

    public string ProductTypeCodeSnapshot { get; private set; } = string.Empty;

    public Guid CharacteristicDefinitionId { get; private set; }

    public string CharacteristicCodeSnapshot { get; private set; } = string.Empty;

    public string ProposedValue { get; private set; } = string.Empty;

    public string CandidateKey { get; private set; } = string.Empty;

    public int OccurrenceCount { get; private set; }

    public int AcceptedCount { get; private set; }

    public int CorrectedCount { get; private set; }

    public int RejectedCount { get; private set; }

    public int DistinctProductCount { get; private set; }

    public CatalogRecognitionCandidateStatus Status { get; private set; }

    public Guid? SuggestionId { get; private set; }

    public DateTime FirstSeenAtUtc { get; private set; }

    public DateTime LastSeenAtUtc { get; private set; }

    public uint Version { get; private set; }

    public bool IsAccumulating => Status == CatalogRecognitionCandidateStatus.Accumulating;

    public bool HasSuggestion => SuggestionId.HasValue;

    public static Result<CatalogRecognitionCandidate, DomainError> Create(
        string phrase,
        string normalizedPhrase,
        Guid productTypeId,
        string productTypeCodeSnapshot,
        Guid characteristicDefinitionId,
        string characteristicCodeSnapshot,
        string proposedValue,
        CatalogRecognitionFeedbackType firstEvidenceType,
        DateTime firstSeenAtUtc)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return GeneralErrors.ValueIsRequired(nameof(phrase));
        }

        if (string.IsNullOrWhiteSpace(normalizedPhrase))
        {
            return GeneralErrors.ValueIsRequired(nameof(normalizedPhrase));
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

        if (string.IsNullOrWhiteSpace(proposedValue))
        {
            return GeneralErrors.ValueIsRequired(nameof(proposedValue));
        }

        if (!IsSupportedEvidenceType(firstEvidenceType))
        {
            return GeneralErrors.ValueIsInvalid(nameof(firstEvidenceType));
        }

        if (firstSeenAtUtc == default)
        {
            return GeneralErrors.ValueIsInvalid(nameof(firstSeenAtUtc));
        }

        var trimmedPhrase = phrase.Trim();
        var trimmedNormalizedPhrase = normalizedPhrase.Trim();
        var trimmedProductTypeCodeSnapshot = productTypeCodeSnapshot.Trim();
        var trimmedCharacteristicCodeSnapshot = characteristicCodeSnapshot.Trim();
        var trimmedProposedValue = proposedValue.Trim();

        if (trimmedPhrase.Length > PhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(phrase), PhraseMaxLength);
        }

        if (trimmedNormalizedPhrase.Length > PhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(normalizedPhrase), PhraseMaxLength);
        }

        if (trimmedProductTypeCodeSnapshot.Length > ProductTypeCodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(productTypeCodeSnapshot), ProductTypeCodeMaxLength);
        }

        if (trimmedCharacteristicCodeSnapshot.Length > CharacteristicCodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(characteristicCodeSnapshot), CharacteristicCodeMaxLength);
        }

        if (trimmedProposedValue.Length > ProposedValueMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(proposedValue), ProposedValueMaxLength);
        }

        var acceptedCount = firstEvidenceType == CatalogRecognitionFeedbackType.Accepted ? 1 : 0;
        var correctedCount = firstEvidenceType == CatalogRecognitionFeedbackType.Corrected ? 1 : 0;
        var rejectedCount = firstEvidenceType == CatalogRecognitionFeedbackType.Rejected ? 1 : 0;

        var candidateKey = BuildCandidateKey(trimmedNormalizedPhrase, productTypeId, characteristicDefinitionId, trimmedProposedValue);

        return new CatalogRecognitionCandidate(
            Guid.CreateVersion7(),
            trimmedPhrase,
            trimmedNormalizedPhrase,
            productTypeId,
            trimmedProductTypeCodeSnapshot,
            characteristicDefinitionId,
            trimmedCharacteristicCodeSnapshot,
            trimmedProposedValue,
            candidateKey,
            occurrenceCount: 1,
            acceptedCount,
            correctedCount,
            rejectedCount,
            distinctProductCount: 1,
            CatalogRecognitionCandidateStatus.Accumulating,
            suggestionId: null,
            firstSeenAtUtc,
            firstSeenAtUtc);
    }

    public UnitResult<DomainError> AddEvidence(CatalogRecognitionFeedbackType feedbackType, DateTime seenAtUtc, bool isDistinctProduct)
    {
        if (!IsSupportedEvidenceType(feedbackType))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(feedbackType)));
        }

        if (seenAtUtc == default)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(seenAtUtc)));
        }

        if (OccurrenceCount == int.MaxValue)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(OccurrenceCount)));
        }

        if (isDistinctProduct && DistinctProductCount == int.MaxValue)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(DistinctProductCount)));
        }

        OccurrenceCount++;

        switch (feedbackType)
        {
            case CatalogRecognitionFeedbackType.Accepted:
                AcceptedCount++;
                break;

            case CatalogRecognitionFeedbackType.Corrected:
                CorrectedCount++;
                break;

            case CatalogRecognitionFeedbackType.Rejected:
                RejectedCount++;
                break;
        }

        if (isDistinctProduct)
        {
            DistinctProductCount++;
        }

        if (seenAtUtc < FirstSeenAtUtc)
        {
            FirstSeenAtUtc = seenAtUtc;
        }

        if (seenAtUtc > LastSeenAtUtc)
        {
            LastSeenAtUtc = seenAtUtc;
        }

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> AttachSuggestion(Guid suggestionId)
    {
        if (suggestionId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(suggestionId)));
        }

        if (Status == CatalogRecognitionCandidateStatus.SuggestionCreated && SuggestionId == suggestionId)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status != CatalogRecognitionCandidateStatus.Accumulating)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        Status = CatalogRecognitionCandidateStatus.SuggestionCreated;
        SuggestionId = suggestionId;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkApproved()
    {
        if (Status == CatalogRecognitionCandidateStatus.Approved)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status != CatalogRecognitionCandidateStatus.SuggestionCreated || !SuggestionId.HasValue)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        Status = CatalogRecognitionCandidateStatus.Approved;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkRejected()
    {
        if (Status == CatalogRecognitionCandidateStatus.Rejected)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status != CatalogRecognitionCandidateStatus.SuggestionCreated || !SuggestionId.HasValue)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        Status = CatalogRecognitionCandidateStatus.Rejected;

        return UnitResult.Success<DomainError>();
    }

    public static string BuildCandidateKey(string normalizedPhrase, Guid productTypeId, Guid characteristicDefinitionId, string proposedValue)
    {
        var normalizedPhraseBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(normalizedPhrase));
        var proposedValueBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(proposedValue));

        var keySource = string.Join(
            "|",
            normalizedPhraseBase64,
            productTypeId.ToString("N"),
            characteristicDefinitionId.ToString("N"),
            proposedValueBase64);

        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(keySource));

        return Convert.ToHexString(keyBytes);
    }

    private static bool IsSupportedEvidenceType(CatalogRecognitionFeedbackType feedbackType)
    {
        return feedbackType is CatalogRecognitionFeedbackType.Accepted
            or CatalogRecognitionFeedbackType.Corrected
            or CatalogRecognitionFeedbackType.Rejected;
    }
}