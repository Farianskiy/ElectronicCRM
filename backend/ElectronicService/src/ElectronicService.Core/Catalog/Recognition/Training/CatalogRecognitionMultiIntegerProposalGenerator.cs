using System.Text.Json;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionMultiIntegerProposalGenerator
{
    public const string Version = "multi-integer-tokens-v1";

    public static CatalogRecognitionMultiIntegerProposalSet Generate(
        CatalogRecognitionMultiIntegerSampleSet source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanGenerate)
        {
            return new CatalogRecognitionMultiIntegerProposalSet(
                source.ManufacturerId,
                source.ProductTypeId,
                Version,
                [],
                source.Issues);
        }

        var issues = new List<CatalogRecognitionTrainingIssue>();

        if (source.Samples.Count > 1000)
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "TooManyCompleteNames",
                "Для одного запуска допускается не более 1000 полностью размеченных названий.",
                Array.Empty<Guid>()));

            return new CatalogRecognitionMultiIntegerProposalSet(
                source.ManufacturerId,
                source.ProductTypeId,
                Version,
                [],
                issues);
        }

        var patterns = new Dictionary<string, CatalogRecognitionMultiIntegerPattern>(StringComparer.Ordinal);

        foreach (var sample in source.Samples)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pattern = CreatePattern(sample);

            if (!CatalogRecognitionMultiIntegerPatternValidator.IsValid(pattern))
            {
                issues.Add(new CatalogRecognitionTrainingIssue(
                    "InvalidGeneratedPattern",
                    $"Не удалось построить допустимую структуру для «{sample.ProductName}».",
                    GetExampleIds(sample)));
                continue;
            }

            var key = JsonSerializer.Serialize(pattern);
            patterns.TryAdd(key, pattern);

            if (patterns.Count > 100)
            {
                issues.Add(new CatalogRecognitionTrainingIssue(
                    "TooManyStructures",
                    "Найдено более 100 разных структур. Разделите учебную подборку по сериям или форматам названия.",
                    Array.Empty<Guid>()));
                break;
            }
        }

        if (issues.Count > 0)
        {
            return new CatalogRecognitionMultiIntegerProposalSet(
                source.ManufacturerId,
                source.ProductTypeId,
                Version,
                [],
                issues);
        }

        var proposals = new List<CatalogRecognitionMultiIntegerProposal>();

        foreach (var pattern in patterns.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Value))
        {
            cancellationToken.ThrowIfCancellationRequested();

            proposals.Add(Evaluate(pattern, source.Samples, cancellationToken));
        }

        return new CatalogRecognitionMultiIntegerProposalSet(
            source.ManufacturerId,
            source.ProductTypeId,
            Version,
            proposals,
            issues);
    }

    private static CatalogRecognitionMultiIntegerPattern CreatePattern(
        CatalogRecognitionMultiIntegerSample sample)
    {
        var annotationsByToken = sample.Annotations.ToDictionary(annotation => annotation.TokenIndex);
        var parts = new List<CatalogRecognitionMultiIntegerPart>(sample.Tokens.Count);

        for (var index = 0; index < sample.Tokens.Count; index++)
        {
            if (annotationsByToken.TryGetValue(index, out var annotation))
            {
                parts.Add(new CatalogRecognitionMultiIntegerPart(null, annotation.CharacteristicDefinitionId));
                continue;
            }

            parts.Add(new CatalogRecognitionMultiIntegerPart(sample.Tokens[index].Text, null));
        }

        return new CatalogRecognitionMultiIntegerPattern(parts);
    }

    private static CatalogRecognitionMultiIntegerProposal Evaluate(
        CatalogRecognitionMultiIntegerPattern pattern,
        IReadOnlyList<CatalogRecognitionMultiIntegerSample> samples,
        CancellationToken cancellationToken)
    {
        var characteristicIds = pattern.Parts
            .Where(part => part.CharacteristicDefinitionId.HasValue)
            .Select(part => part.CharacteristicDefinitionId.GetValueOrDefault())
            .OrderBy(id => id)
            .ToArray();

        var valuesByCharacteristic = characteristicIds.ToDictionary(
            id => id,
            _ => new HashSet<string>(StringComparer.Ordinal));

        var supportingIds = new HashSet<Guid>();
        var conflictingIds = new HashSet<Guid>();
        var supportingNames = new HashSet<string>(StringComparer.Ordinal);
        var matchedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = CatalogRecognitionMultiIntegerMatcher.Match(sample.ProductName, pattern);

            if (string.Equals(result.Status, "NoMatch", StringComparison.Ordinal))
            {
                continue;
            }

            var exampleIds = GetExampleIds(sample);

            if (!string.Equals(result.Status, "Matched", StringComparison.Ordinal))
            {
                conflictingIds.UnionWith(exampleIds);
                continue;
            }

            matchedNames.Add(sample.ProductName);

            if (!MatchesAnnotations(sample, result.Captures))
            {
                conflictingIds.UnionWith(exampleIds);
                continue;
            }

            supportingNames.Add(sample.ProductName);
            supportingIds.UnionWith(exampleIds);

            foreach (var capture in result.Captures)
            {
                valuesByCharacteristic[capture.CharacteristicDefinitionId].Add(capture.NormalizedValue);
            }
        }

        var fields = characteristicIds.Select(id => new CatalogRecognitionMultiIntegerFieldCoverage(
            id,
            valuesByCharacteristic[id].Count)).ToArray();

        return new CatalogRecognitionMultiIntegerProposal(
            pattern,
            matchedNames.Count,
            supportingNames.Count,
            fields,
            supportingIds.OrderBy(id => id).ToArray(),
            conflictingIds.OrderBy(id => id).ToArray());
    }

    private static bool MatchesAnnotations(
        CatalogRecognitionMultiIntegerSample sample,
        IReadOnlyList<CatalogRecognitionMultiIntegerCapture> captures)
    {
        if (captures.Count != sample.Annotations.Count)
        {
            return false;
        }

        foreach (var capture in captures)
        {
            var annotation = sample.Annotations.SingleOrDefault(item => item.CharacteristicDefinitionId == capture.CharacteristicDefinitionId);

            if (annotation is null)
            {
                return false;
            }

            if (annotation.SpanStart != capture.SpanStart || annotation.SpanLength != capture.SpanLength)
            {
                return false;
            }

            if (!string.Equals(annotation.RawValue, capture.RawValue, StringComparison.Ordinal) || !string.Equals(annotation.NormalizedValue, capture.NormalizedValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static Guid[] GetExampleIds(CatalogRecognitionMultiIntegerSample sample)
    {
        return sample.Annotations
            .SelectMany(annotation => annotation.ExampleIds)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
    }
}