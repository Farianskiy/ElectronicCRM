using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Manufacturers;

public sealed class ManufacturerAlias : AggregateRoot
{
    public const int PhraseMaxLength = 200;

    private ManufacturerAlias(
        Guid id,
        Guid manufacturerId,
        string phrase,
        string normalizedPhrase,
        ManufacturerAliasStatus status,
        ManufacturerAliasSource source,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? approvedAtUtc,
        DateTime? rejectedAtUtc)
        : base(id)
    {
        ManufacturerId = manufacturerId;
        Phrase = phrase;
        NormalizedPhrase = normalizedPhrase;
        Status = status;
        Source = source;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ApprovedAtUtc = approvedAtUtc;
        RejectedAtUtc = rejectedAtUtc;
    }

    private ManufacturerAlias()
    {
    }

    public Guid ManufacturerId { get; private set; }

    public string Phrase { get; private set; } = string.Empty;

    public string NormalizedPhrase { get; private set; } = string.Empty;

    public ManufacturerAliasStatus Status { get; private set; }

    public ManufacturerAliasSource Source { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public DateTime? RejectedAtUtc { get; private set; }

    public static Result<ManufacturerAlias, DomainError> Create(
        Guid manufacturerId,
        string phrase,
        ManufacturerAliasStatus status,
        ManufacturerAliasSource source)
    {
        if (manufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (string.IsNullOrWhiteSpace(phrase))
        {
            return GeneralErrors.ValueIsRequired(nameof(phrase));
        }

        var trimmedPhrase = phrase.Trim();

        if (trimmedPhrase.Length > PhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(phrase), PhraseMaxLength);
        }

        if (!Enum.IsDefined(status) || status == ManufacturerAliasStatus.None)
        {
            return ManufacturerAliasErrors.StatusIsInvalid(status);
        }

        if (!Enum.IsDefined(source) || source == ManufacturerAliasSource.None)
        {
            return ManufacturerAliasErrors.SourceIsInvalid(source);
        }

        var normalizedPhrase = ManufacturerNameNormalizer.Normalize(trimmedPhrase);
        var createdAtUtc = DateTime.UtcNow;

        return new ManufacturerAlias(
            Guid.CreateVersion7(),
            manufacturerId,
            trimmedPhrase,
            normalizedPhrase,
            status,
            source,
            createdAtUtc,
            createdAtUtc,
            status == ManufacturerAliasStatus.Approved ? createdAtUtc : null,
            status == ManufacturerAliasStatus.Rejected ? createdAtUtc : null);
    }

    public UnitResult<DomainError> Approve()
    {
        if (Status == ManufacturerAliasStatus.Approved)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status == ManufacturerAliasStatus.Rejected)
        {
            return ManufacturerAliasErrors.RejectedAliasCannotBeApproved(Id);
        }

        var approvedAtUtc = DateTime.UtcNow;

        Status = ManufacturerAliasStatus.Approved;
        UpdatedAtUtc = approvedAtUtc;
        ApprovedAtUtc = approvedAtUtc;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reject()
    {
        if (Status == ManufacturerAliasStatus.Rejected)
        {
            return UnitResult.Success<DomainError>();
        }

        if (Status == ManufacturerAliasStatus.Approved)
        {
            return ManufacturerAliasErrors.ApprovedAliasCannotBeRejected(Id);
        }

        var rejectedAtUtc = DateTime.UtcNow;

        Status = ManufacturerAliasStatus.Rejected;
        UpdatedAtUtc = rejectedAtUtc;
        RejectedAtUtc = rejectedAtUtc;

        return UnitResult.Success<DomainError>();
    }
}