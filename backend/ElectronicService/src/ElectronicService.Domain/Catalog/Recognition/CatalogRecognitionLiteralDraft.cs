using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionLiteralDraft : AggregateRoot
{
    private readonly List<CatalogRecognitionLiteralDraftEvidence> _evidence = [];

    private CatalogRecognitionLiteralDraft()
    {
    }

    private CatalogRecognitionLiteralDraft(Guid id) : base(id)
    {
    }

    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public Guid CharacteristicDefinitionId { get; private set; }
    public string Literal { get; private set; } = string.Empty;
    public string NormalizedValue { get; private set; } = string.Empty;
    public string GeneratorVersion { get; private set; } = string.Empty;
    public int MatchedNameCount { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CatalogRecognitionLiteralDraftEvidence> Evidence => _evidence.AsReadOnly();

    public static Result<CatalogRecognitionLiteralDraft, DomainError> Create(
        Guid manufacturerId,
        Guid productTypeId,
        Guid characteristicDefinitionId,
        string literal,
        string normalizedValue,
        string generatorVersion,
        int matchedNameCount,
        Guid createdByUserId,
        IReadOnlyCollection<Guid> checkedExampleIds,
        IReadOnlyCollection<Guid> supportingExampleIds)
    {
        ArgumentNullException.ThrowIfNull(checkedExampleIds);
        ArgumentNullException.ThrowIfNull(supportingExampleIds);

        if (manufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (createdByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(createdByUserId));
        }

        if (string.IsNullOrWhiteSpace(literal))
        {
            return GeneralErrors.ValueIsRequired(nameof(literal));
        }

        if (literal.Length > 2000)
        {
            return GeneralErrors.ValueIsTooLong(nameof(literal), 2000);
        }

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return GeneralErrors.ValueIsRequired(nameof(normalizedValue));
        }

        if (normalizedValue.Length > 2000)
        {
            return GeneralErrors.ValueIsTooLong(nameof(normalizedValue), 2000);
        }

        if (string.IsNullOrWhiteSpace(generatorVersion))
        {
            return GeneralErrors.ValueIsRequired(nameof(generatorVersion));
        }

        if (generatorVersion.Length > 100)
        {
            return GeneralErrors.ValueIsTooLong(nameof(generatorVersion), 100);
        }

        if (checkedExampleIds.Count == 0 || checkedExampleIds.Count > 1000 || checkedExampleIds.Contains(Guid.Empty))
        {
            return GeneralErrors.ValueIsInvalid(nameof(checkedExampleIds));
        }

        var checkedIds = checkedExampleIds.ToHashSet();
        var supportingIds = supportingExampleIds.ToHashSet();

        if (supportingIds.Count == 0 || !supportingIds.IsSubsetOf(checkedIds))
        {
            return GeneralErrors.ValueIsInvalid(nameof(supportingExampleIds));
        }

        if (matchedNameCount <= 0 || matchedNameCount > supportingIds.Count)
        {
            return GeneralErrors.ValueIsInvalid(nameof(matchedNameCount));
        }

        var draft = new CatalogRecognitionLiteralDraft(Guid.CreateVersion7())
        {
            ManufacturerId = manufacturerId,
            ProductTypeId = productTypeId,
            CharacteristicDefinitionId = characteristicDefinitionId,
            Literal = literal,
            NormalizedValue = normalizedValue,
            GeneratorVersion = generatorVersion,
            MatchedNameCount = matchedNameCount,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var exampleId in checkedIds.OrderBy(id => id))
        {
            draft._evidence.Add(new CatalogRecognitionLiteralDraftEvidence(draft.Id, exampleId, supportingIds.Contains(exampleId)));
        }

        return draft;
    }
}