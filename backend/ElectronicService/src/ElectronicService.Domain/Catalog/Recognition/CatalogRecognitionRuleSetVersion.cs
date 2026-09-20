using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionRuleSetVersion : AggregateRoot
{
    private readonly List<CatalogRecognitionRuleSetEntry> _entries = [];

    private CatalogRecognitionRuleSetVersion()
    {
    }

    private CatalogRecognitionRuleSetVersion(Guid id) : base(id)
    {
    }

    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CatalogRecognitionRuleSetEntry> Entries => _entries.AsReadOnly();

    public static Result<CatalogRecognitionRuleSetVersion, DomainError> Create(
        CatalogRecognitionRuleSetVersionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.Entries);

        if (data.ManufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.ManufacturerId));
        }

        if (data.ProductTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.ProductTypeId));
        }

        if (data.CreatedByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.CreatedByUserId));
        }

        if (data.VersionNumber < 1)
        {
            return GeneralErrors.ValueIsInvalid(nameof(data.VersionNumber));
        }

        if (string.IsNullOrWhiteSpace(data.Name))
        {
            return GeneralErrors.ValueIsRequired(nameof(data.Name));
        }

        if (data.Name.Length > 200)
        {
            return GeneralErrors.ValueIsTooLong(nameof(data.Name), 200);
        }

        if (data.Entries.Count == 0 || data.Entries.Count > 100)
        {
            return new DomainError(
                "training.invalid_request",
                "Версия должна содержать от 1 до 100 шаблонов.");
        }

        var references = new HashSet<(CatalogRecognitionRuleKind Kind, Guid DraftId)>();

        foreach (var entry in data.Entries)
        {
            if (entry is null || entry.Kind == CatalogRecognitionRuleKind.None || !Enum.IsDefined(entry.Kind) || entry.DraftId == Guid.Empty)
            {
                return new DomainError(
                    "training.invalid_request",
                    "Укажите допустимый вид и идентификатор каждого шаблона.");
            }

            if (!references.Add((entry.Kind, entry.DraftId)))
            {
                return new DomainError(
                    "training.invalid_request",
                    "Один шаблон не должен повторяться в составе версии.");
            }
        }

        var version = new CatalogRecognitionRuleSetVersion(Guid.CreateVersion7())
        {
            ManufacturerId = data.ManufacturerId,
            ProductTypeId = data.ProductTypeId,
            VersionNumber = data.VersionNumber,
            Name = data.Name.Trim(),
            CreatedByUserId = data.CreatedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        var orderedEntries = data.Entries
            .OrderBy(entry => entry.Kind)
            .ThenBy(entry => entry.DraftId)
            .ToArray();

        for (var position = 0; position < orderedEntries.Length; position++)
        {
            version._entries.Add(new CatalogRecognitionRuleSetEntry(
                version.Id,
                position,
                orderedEntries[position]));
        }

        return version;
    }
}