using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed class ManufacturerResolutionIndex
{
    private const decimal ExactNameConfidence = 0.9900m;
    private const decimal ApprovedAliasConfidence = 0.9800m;

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

    public ManufacturerNameRecognitionResult RecognizeInText(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return ManufacturerNameRecognitionResult.Unresolved(productName ?? string.Empty);
        }

        var normalizedProductName = NormalizeTextPreservingLength(productName);
        var candidates = new List<ManufacturerNameRecognitionCandidate>();

        foreach (var entry in _entriesByNormalizedInputName.Values)
        {
            foreach (var startIndex in FindWholeTermStartIndexes(normalizedProductName, entry.NormalizedInputName))
            {
                candidates.Add(new ManufacturerNameRecognitionCandidate(
                    entry.ManufacturerId,
                    entry.ManufacturerName,
                    productName.Substring(startIndex, entry.NormalizedInputName.Length),
                    entry.NormalizedInputName,
                    GetConfidence(entry.Source),
                    entry.Source,
                    entry.ManufacturerAliasId,
                    startIndex,
                    entry.NormalizedInputName.Length));
            }
        }

        var orderedCandidates = candidates
            .OrderByDescending(candidate => GetSourcePrecedence(candidate.Source))
            .ThenByDescending(candidate => candidate.Length)
            .ThenBy(candidate => candidate.StartIndex)
            .ThenBy(candidate => candidate.ManufacturerName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.NormalizedValue, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();

        if (orderedCandidates.Count == 0)
        {
            return ManufacturerNameRecognitionResult.Unresolved(productName);
        }

        var distinctManufacturerCount = orderedCandidates
            .Select(candidate => candidate.ManufacturerId)
            .Distinct()
            .Take(2)
            .Count();

        if (distinctManufacturerCount > 1)
        {
            return ManufacturerNameRecognitionResult.Conflict(productName, orderedCandidates);
        }

        return ManufacturerNameRecognitionResult.Resolved(
            productName,
            orderedCandidates[0],
            orderedCandidates);
    }

    private static IEnumerable<int> FindWholeTermStartIndexes(string text, string term)
    {
        if (string.IsNullOrEmpty(term) || term.Length > text.Length)
        {
            yield break;
        }

        var searchStartIndex = 0;

        while (searchStartIndex <= text.Length - term.Length)
        {
            var matchIndex = text.IndexOf(
                term,
                searchStartIndex,
                StringComparison.Ordinal);

            if (matchIndex < 0)
            {
                yield break;
            }

            if (HasWholeTokenBoundaries(text, matchIndex, term.Length))
            {
                yield return matchIndex;
            }

            searchStartIndex = matchIndex + 1;
        }
    }

    private static bool HasWholeTokenBoundaries(string text, int startIndex, int length)
    {
        var hasStartBoundary =
            startIndex == 0 ||
            !IsTokenCharacter(text[startIndex - 1]);

        var endIndex = startIndex + length;

        var hasEndBoundary =
            endIndex == text.Length ||
            !IsTokenCharacter(text[endIndex]);

        return hasStartBoundary && hasEndBoundary;
    }

    private static bool IsTokenCharacter(char character)
    {
        return char.IsLetterOrDigit(character);
    }

    private static string NormalizeTextPreservingLength(string value)
    {
        var normalizedCharacters = value.ToCharArray();

        for (var index = 0; index < normalizedCharacters.Length; index++)
        {
            normalizedCharacters[index] = normalizedCharacters[index] switch
            {
                'ё' or 'Ё' => 'Е',
                _ => char.ToUpperInvariant(normalizedCharacters[index])
            };
        }

        return new string(normalizedCharacters);
    }

    private static decimal GetConfidence(ManufacturerResolutionSource source)
    {
        return source switch
        {
            ManufacturerResolutionSource.ExactName => ExactNameConfidence,
            ManufacturerResolutionSource.ApprovedAlias => ApprovedAliasConfidence,
            _ => throw new InvalidOperationException($"Unsupported manufacturer resolution source: {source}.")
        };
    }

    private static int GetSourcePrecedence(ManufacturerResolutionSource source)
    {
        return source switch
        {
            ManufacturerResolutionSource.ExactName => 2,
            ManufacturerResolutionSource.ApprovedAlias => 1,
            _ => 0
        };
    }
}