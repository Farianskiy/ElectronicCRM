using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionMultiIntegerDraft : AggregateRoot
{
    private readonly List<CatalogRecognitionMultiIntegerDraftPart> _parts = [];
    private readonly List<CatalogRecognitionMultiIntegerDraftEvidence> _evidence = [];

    private CatalogRecognitionMultiIntegerDraft()
    {
    }

    private CatalogRecognitionMultiIntegerDraft(Guid id) : base(id)
    {
    }

    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public string GeneratorVersion { get; private set; } = string.Empty;
    public int MatchedNameCount { get; private set; }
    public int SupportingNameCount { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CatalogRecognitionMultiIntegerDraftPart> Parts => _parts.AsReadOnly();
    public IReadOnlyCollection<CatalogRecognitionMultiIntegerDraftEvidence> Evidence => _evidence.AsReadOnly();

    public static Result<CatalogRecognitionMultiIntegerDraft, DomainError> Create(
        CatalogRecognitionMultiIntegerDraftData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.Parts);
        ArgumentNullException.ThrowIfNull(data.CheckedExampleIds);
        ArgumentNullException.ThrowIfNull(data.SupportingExampleIds);

        if (data.ManufacturerId == Guid.Empty || data.ProductTypeId == Guid.Empty || data.CreatedByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data));
        }

        if (string.IsNullOrWhiteSpace(data.GeneratorVersion))
        {
            return GeneralErrors.ValueIsRequired(nameof(data.GeneratorVersion));
        }

        if (data.GeneratorVersion.Length > 100)
        {
            return GeneralErrors.ValueIsTooLong(nameof(data.GeneratorVersion), 100);
        }

        if (data.SupportingNameCount < 2 || data.MatchedNameCount != data.SupportingNameCount || data.MatchedNameCount > 200)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.SupportingNameCount));
        }

        if (!ArePartsValid(data.Parts, data.SupportingNameCount))
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.Parts));
        }

        if (data.CheckedExampleIds.Count == 0 || data.CheckedExampleIds.Count > 16000 || data.CheckedExampleIds.Contains(Guid.Empty))
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.CheckedExampleIds));
        }

        var checkedIds = data.CheckedExampleIds.ToHashSet();
        var supportingIds = data.SupportingExampleIds.ToHashSet();

        if (checkedIds.Count != data.CheckedExampleIds.Count)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.CheckedExampleIds));
        }

        if (supportingIds.Count != data.SupportingExampleIds.Count || !supportingIds.IsSubsetOf(checkedIds))
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.SupportingExampleIds));
        }

        var captureCount = data.Parts.Count(part => part.CharacteristicDefinitionId.HasValue);

        if (supportingIds.Count < data.SupportingNameCount * captureCount)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.SupportingExampleIds));
        }

        var draft = new CatalogRecognitionMultiIntegerDraft(Guid.CreateVersion7())
        {
            ManufacturerId = data.ManufacturerId,
            ProductTypeId = data.ProductTypeId,
            GeneratorVersion = data.GeneratorVersion,
            MatchedNameCount = data.MatchedNameCount,
            SupportingNameCount = data.SupportingNameCount,
            CreatedByUserId = data.CreatedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        for (var position = 0; position < data.Parts.Count; position++)
        {
            var part = data.Parts[position];

            draft._parts.Add(new CatalogRecognitionMultiIntegerDraftPart(
                draft.Id,
                position,
                part.Literal,
                part.CharacteristicDefinitionId,
                part.DistinctValueCount));
        }

        foreach (var exampleId in checkedIds.OrderBy(id => id))
        {
            draft._evidence.Add(new CatalogRecognitionMultiIntegerDraftEvidence(
                draft.Id,
                exampleId,
                supportingIds.Contains(exampleId)));
        }

        return draft;
    }

    private static bool ArePartsValid(
        IReadOnlyList<CatalogRecognitionMultiIntegerDraftPartData> parts,
        int supportingNameCount)
    {
        if (parts.Count == 0 || parts.Count > 256)
        {
            return false;
        }

        var characteristicIds = new HashSet<Guid>();
        var literalLength = 0;
        var previousWasCapture = false;
        var hasLiteral = false;

        foreach (var part in parts)
        {
            if (part is null)
            {
                return false;
            }

            if (part.CharacteristicDefinitionId is Guid characteristicId)
            {
                if (characteristicId == Guid.Empty || part.Literal is not null || previousWasCapture || !characteristicIds.Add(characteristicId))
                {
                    return false;
                }

                if (part.DistinctValueCount < 2 || part.DistinctValueCount > supportingNameCount)
                {
                    return false;
                }

                previousWasCapture = true;
                continue;
            }

            if (string.IsNullOrEmpty(part.Literal) || part.Literal.Length > 2000 || part.DistinctValueCount != 0)
            {
                return false;
            }

            literalLength += part.Literal.Length;

            if (literalLength > 2000)
            {
                return false;
            }

            hasLiteral = true;
            previousWasCapture = false;
        }

        return hasLiteral && characteristicIds.Count >= 2 && characteristicIds.Count <= 16;
    }
}