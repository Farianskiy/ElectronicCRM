using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Manufacturers;

public sealed class ManufacturerNoisePhrase : AggregateRoot
{
    public const int PhraseMaxLength = 200;

    public const int ReasonMaxLength = 500;

    private ManufacturerNoisePhrase(
        Guid id,
        string phrase,
        string normalizedPhrase,
        string? reason,
        bool isActive,
        Guid createdByUserId,
        Guid updatedByUserId,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? deactivatedAtUtc)
        : base(id)
    {
        Phrase = phrase;
        NormalizedPhrase = normalizedPhrase;
        Reason = reason;
        IsActive = isActive;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = updatedByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        DeactivatedAtUtc = deactivatedAtUtc;
    }

    private ManufacturerNoisePhrase()
    {
    }

    public string Phrase { get; private set; } = string.Empty;

    public string NormalizedPhrase { get; private set; } = string.Empty;

    public string? Reason { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? DeactivatedAtUtc { get; private set; }

    public static Result<ManufacturerNoisePhrase, DomainError> Create(string phrase, string? reason, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return GeneralErrors.ValueIsRequired(nameof(phrase));
        }

        if (createdByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(createdByUserId));
        }

        var trimmedPhrase = phrase.Trim();

        if (trimmedPhrase.Length > PhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(phrase), PhraseMaxLength);
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();

        if (trimmedReason is not null && trimmedReason.Length > ReasonMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(reason), ReasonMaxLength);
        }

        var normalizedPhrase = ManufacturerNameNormalizer.Normalize(trimmedPhrase);
        var createdAtUtc = DateTime.UtcNow;

        return new ManufacturerNoisePhrase(
            Guid.CreateVersion7(),
            trimmedPhrase,
            normalizedPhrase,
            trimmedReason,
            true,
            createdByUserId,
            createdByUserId,
            createdAtUtc,
            createdAtUtc,
            null);
    }

    public UnitResult<DomainError> UpdateReason(string? reason, Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(updatedByUserId)));
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();

        if (trimmedReason is not null && trimmedReason.Length > ReasonMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(nameof(reason), ReasonMaxLength));
        }

        Reason = trimmedReason;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Activate(Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(updatedByUserId)));
        }

        if (IsActive)
        {
            return UnitResult.Success<DomainError>();
        }

        var updatedAtUtc = DateTime.UtcNow;

        IsActive = true;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
        DeactivatedAtUtc = null;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Deactivate(Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(updatedByUserId)));
        }

        if (!IsActive)
        {
            return UnitResult.Success<DomainError>();
        }

        var updatedAtUtc = DateTime.UtcNow;

        IsActive = false;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
        DeactivatedAtUtc = updatedAtUtc;

        return UnitResult.Success<DomainError>();
    }
}