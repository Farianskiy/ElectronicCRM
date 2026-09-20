using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionTrainingExample : AggregateRoot
{
    private CatalogRecognitionTrainingExample()
    {
    }

    private CatalogRecognitionTrainingExample(Guid id) : base(id)
    {
    }

    public Guid SourceFeedbackId { get; private set; }
    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public Guid CharacteristicDefinitionId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string RawValue { get; private set; } = string.Empty;
    public int SpanStart { get; private set; }
    public int SpanLength { get; private set; }
    public string NormalizedValue { get; private set; } = string.Empty;
    public Guid ConfirmedByUserId { get; private set; }
    public DateTime ConfirmedAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public static Result<CatalogRecognitionTrainingExample, DomainError> Create(CatalogRecognitionFeedback feedback, Guid confirmedByUserId)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        if (confirmedByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(confirmedByUserId));
        }

        if (!feedback.ManufacturerId.HasValue || feedback.ManufacturerId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedback.ManufacturerId));
        }

        if (feedback.ProductTypeId == Guid.Empty || feedback.CharacteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedback));
        }

        if (!Enum.IsDefined(feedback.FeedbackType) || feedback.FeedbackType == CatalogRecognitionFeedbackType.None || feedback.FeedbackType == CatalogRecognitionFeedbackType.Rejected)
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedback.FeedbackType));
        }

        var rawValue = feedback.ConfirmedRawValue;
        var normalizedValue = feedback.FinalNormalizedValue;

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return GeneralErrors.ValueIsRequired(nameof(feedback.FinalNormalizedValue));
        }

        if (string.IsNullOrWhiteSpace(rawValue) || !feedback.ConfirmedSpanStart.HasValue || !feedback.ConfirmedSpanLength.HasValue)
        {
            return GeneralErrors.ValueIsRequired(nameof(feedback.ConfirmedRawValue));
        }

        var start = feedback.ConfirmedSpanStart.Value;
        var length = feedback.ConfirmedSpanLength.Value;

        if (start < 0 || start >= feedback.ProductName.Length || length <= 0 || length > feedback.ProductName.Length - start)
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedback.ConfirmedSpanLength));
        }

        var end = start + length;

        if ((start > 0 && char.IsLowSurrogate(feedback.ProductName[start]) && char.IsHighSurrogate(feedback.ProductName[start - 1])) || (end < feedback.ProductName.Length && char.IsLowSurrogate(feedback.ProductName[end]) && char.IsHighSurrogate(feedback.ProductName[end - 1])))
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedback.ConfirmedSpanLength));
        }

        if (!string.Equals(feedback.ProductName.Substring(start, length), rawValue, StringComparison.Ordinal))
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedback.ConfirmedRawValue));
        }

        return new CatalogRecognitionTrainingExample(Guid.CreateVersion7())
        {
            SourceFeedbackId = feedback.Id,
            ManufacturerId = feedback.ManufacturerId.Value,
            ProductTypeId = feedback.ProductTypeId,
            CharacteristicDefinitionId = feedback.CharacteristicDefinitionId,
            ProductName = feedback.ProductName,
            RawValue = rawValue,
            SpanStart = start,
            SpanLength = length,
            NormalizedValue = normalizedValue,
            ConfirmedByUserId = confirmedByUserId,
            ConfirmedAtUtc = DateTime.UtcNow
        };
    }

    public UnitResult<DomainError> Revoke(Guid revokedByUserId)
    {
        if (revokedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(revokedByUserId)));
        }

        if (RevokedAtUtc.HasValue)
        {
            return UnitResult.Success<DomainError>();
        }

        RevokedByUserId = revokedByUserId;
        RevokedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }
}