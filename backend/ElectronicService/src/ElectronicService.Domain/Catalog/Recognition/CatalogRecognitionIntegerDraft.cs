using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionIntegerDraft : AggregateRoot
{
    private readonly List<CatalogRecognitionIntegerDraftSuffix> _suffixes = [];
    private readonly List<CatalogRecognitionIntegerDraftEvidence> _evidence = [];

    private CatalogRecognitionIntegerDraft()
    {
    }

    private CatalogRecognitionIntegerDraft(Guid id) : base(id)
    {
    }

    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public Guid CharacteristicDefinitionId { get; private set; }
    public string Prefix { get; private set; } = string.Empty;
    public string GeneratorVersion { get; private set; } = string.Empty;
    public int MatchedNameCount { get; private set; }
    public int DistinctValueCount { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CatalogRecognitionIntegerDraftSuffix> Suffixes => _suffixes.AsReadOnly();
    public IReadOnlyCollection<CatalogRecognitionIntegerDraftEvidence> Evidence => _evidence.AsReadOnly();

    public static Result<CatalogRecognitionIntegerDraft, DomainError> Create(
        Guid manufacturerId,
        Guid productTypeId,
        Guid characteristicDefinitionId,
        string prefix,
        IReadOnlyCollection<string> suffixes,
        string generatorVersion,
        int matchedNameCount,
        int distinctValueCount,
        Guid createdByUserId,
        IReadOnlyCollection<Guid> checkedExampleIds,
        IReadOnlyCollection<Guid> supportingExampleIds)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(suffixes);
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

        if (prefix.Length > 2000)
        {
            return GeneralErrors.ValueIsTooLong(nameof(prefix), 2000);
        }

        if (suffixes.Count == 0 || suffixes.Count > 16)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suffixes));
        }

        if (suffixes.Any(suffix => suffix is null || suffix.Length > 2000))
        {
            return GeneralErrors.ValueIsInvalid(nameof(suffixes));
        }

        if (prefix.Length == 0 && suffixes.Any(suffix => suffix.Length == 0))
        {
            return GeneralErrors.ValueIsInvalid(nameof(suffixes));
        }

        var orderedSuffixes = suffixes.Distinct(StringComparer.Ordinal).OrderBy(suffix => suffix, StringComparer.Ordinal).ToArray();

        if (orderedSuffixes.Length != suffixes.Count)
        {
            return GeneralErrors.ValueIsInvalid(nameof(suffixes));
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

        if (supportingIds.Count < 2 || !supportingIds.IsSubsetOf(checkedIds))
        {
            return GeneralErrors.ValueIsInvalid(nameof(supportingExampleIds));
        }

        if (matchedNameCount < 2 || matchedNameCount > supportingIds.Count)
        {
            return GeneralErrors.ValueIsInvalid(nameof(matchedNameCount));
        }

        if (distinctValueCount < 2 || distinctValueCount > matchedNameCount)
        {
            return GeneralErrors.ValueIsInvalid(nameof(distinctValueCount));
        }

        var draft = new CatalogRecognitionIntegerDraft(Guid.CreateVersion7())
        {
            ManufacturerId = manufacturerId,
            ProductTypeId = productTypeId,
            CharacteristicDefinitionId = characteristicDefinitionId,
            Prefix = prefix,
            GeneratorVersion = generatorVersion,
            MatchedNameCount = matchedNameCount,
            DistinctValueCount = distinctValueCount,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        for (var position = 0; position < orderedSuffixes.Length; position++)
        {
            draft._suffixes.Add(new CatalogRecognitionIntegerDraftSuffix(draft.Id, position, orderedSuffixes[position]));
        }

        foreach (var exampleId in checkedIds.OrderBy(id => id))
        {
            draft._evidence.Add(new CatalogRecognitionIntegerDraftEvidence(draft.Id, exampleId, supportingIds.Contains(exampleId)));
        }

        return draft;
    }
}