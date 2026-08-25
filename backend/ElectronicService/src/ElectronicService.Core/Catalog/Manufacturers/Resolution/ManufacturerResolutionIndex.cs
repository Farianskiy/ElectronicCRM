using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed class ManufacturerResolutionIndex
{
    private readonly Dictionary<string, ManufacturerResolutionEntry> _entriesByNormalizedInputName;
    private readonly Dictionary<string, ManufacturerNoiseResolutionEntry> _noiseEntriesByNormalizedInputName;

    public ManufacturerResolutionIndex(
        IReadOnlyCollection<ManufacturerResolutionEntry> entries,
        IReadOnlyCollection<ManufacturerNoiseResolutionEntry> noiseEntries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(noiseEntries);

        _entriesByNormalizedInputName = new Dictionary<string, ManufacturerResolutionEntry>(StringComparer.Ordinal);
        _noiseEntriesByNormalizedInputName = new Dictionary<string, ManufacturerNoiseResolutionEntry>(StringComparer.Ordinal);

        var orderedEntries = entries
            .OrderBy(entry => entry.Source)
            .ThenBy(entry => entry.ManufacturerName, StringComparer.Ordinal)
            .ToList();

        foreach (var entry in orderedEntries)
        {
            if (entry.ManufacturerId == Guid.Empty)
            {
                throw new ArgumentException("Manufacturer resolution entry contains an empty ManufacturerId.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.ManufacturerName))
            {
                throw new ArgumentException("Manufacturer resolution entry contains an empty ManufacturerName.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.NormalizedInputName))
            {
                throw new ArgumentException("Manufacturer resolution entry contains an empty NormalizedInputName.", nameof(entries));
            }

            if (entry.Source is ManufacturerResolutionSource.None or ManufacturerResolutionSource.IgnoredNoise)
            {
                throw new ArgumentException("Manufacturer resolution entry contains an invalid source.", nameof(entries));
            }

            var normalizedInputName = ManufacturerNameNormalizer.Normalize(entry.NormalizedInputName);

            var normalizedEntry = entry with
            {
                NormalizedInputName = normalizedInputName
            };

            _entriesByNormalizedInputName.TryAdd(normalizedInputName, normalizedEntry);
        }

        foreach (var noiseEntry in noiseEntries)
        {
            if (noiseEntry.ManufacturerNoisePhraseId == Guid.Empty)
            {
                throw new ArgumentException("Manufacturer noise resolution entry contains an empty ManufacturerNoisePhraseId.", nameof(noiseEntries));
            }

            if (string.IsNullOrWhiteSpace(noiseEntry.NormalizedInputName))
            {
                throw new ArgumentException("Manufacturer noise resolution entry contains an empty NormalizedInputName.", nameof(noiseEntries));
            }

            var normalizedInputName = ManufacturerNameNormalizer.Normalize(noiseEntry.NormalizedInputName);

            var normalizedNoiseEntry = noiseEntry with
            {
                NormalizedInputName = normalizedInputName
            };

            _noiseEntriesByNormalizedInputName.TryAdd(normalizedInputName, normalizedNoiseEntry);
        }
    }

    public int Count => _entriesByNormalizedInputName.Count + _noiseEntriesByNormalizedInputName.Count;

    public ManufacturerResolutionResult Resolve(string? inputName)
    {
        if (string.IsNullOrWhiteSpace(inputName))
        {
            return ManufacturerResolutionResult.Unresolved(inputName ?? string.Empty, string.Empty);
        }

        var trimmedInputName = inputName.Trim();
        var normalizedInputName = ManufacturerNameNormalizer.Normalize(trimmedInputName);

        if (_entriesByNormalizedInputName.TryGetValue(normalizedInputName, out var entry))
        {
            return ManufacturerResolutionResult.Resolved(
                trimmedInputName,
                normalizedInputName,
                entry.ManufacturerId,
                entry.ManufacturerName,
                entry.Source,
                entry.ManufacturerAliasId);
        }

        if (_noiseEntriesByNormalizedInputName.TryGetValue(normalizedInputName, out var noiseEntry))
        {
            return ManufacturerResolutionResult.IgnoredNoise(
                trimmedInputName,
                normalizedInputName,
                noiseEntry.ManufacturerNoisePhraseId,
                noiseEntry.Reason);
        }

        return ManufacturerResolutionResult.Unresolved(trimmedInputName, normalizedInputName);
    }
}