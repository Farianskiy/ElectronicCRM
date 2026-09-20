namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionIntegerAlternativesGenerator
{
    public const string Version = "integer-alternatives-v1";

    public static CatalogRecognitionIntegerAlternativesProposalSet Generate(CatalogRecognitionPreparedSampleSet source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanGenerate)
        {
            return new CatalogRecognitionIntegerAlternativesProposalSet(source.Scope, Version, Array.Empty<CatalogRecognitionIntegerAlternativesProposal>(), source.Issues);
        }

        var candidates = new List<CatalogRecognitionIntegerPattern>();
        var diagnostics = new List<CatalogRecognitionTrainingIssue>();

        foreach (var sample in source.Samples)
        {
            var candidate = CreateCandidate(sample);

            if (candidate is null)
            {
                diagnostics.Add(new CatalogRecognitionTrainingIssue("UnsupportedIntegerExample", $"Пример «{sample.ProductName}» не подходит для извлечения одного целого числа. Подтверждённая разметка не изменена.", sample.ExampleIds));
                continue;
            }

            candidates.Add(candidate);
        }

        var proposals = new List<CatalogRecognitionIntegerAlternativesProposal>();
        var groups = candidates.GroupBy(item => item.Prefix, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var suffixes = group.Select(item => item.Suffix).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();

            if (suffixes.Length > 16)
            {
                diagnostics.Add(new CatalogRecognitionTrainingIssue("TooManySuffixes", $"Для начала «{group.Key}» найдено окончаний: {suffixes.Length}. Лимит — 16. Предложение для этой группы не сформировано.", Array.Empty<Guid>()));
                continue;
            }

            var pattern = new CatalogRecognitionIntegerAlternativesPattern(group.Key, suffixes);
            var proposal = Evaluate(pattern, source.Samples);

            if (proposal.DistinctValueCount < 2)
            {
                diagnostics.Add(new CatalogRecognitionTrainingIssue("InsufficientDistinctValues", $"Для начала «{group.Key}» недостаточно разных подтверждённых чисел: {proposal.DistinctValueCount}. Требуется минимум 2.", proposal.SupportingExampleIds));
                continue;
            }

            proposals.Add(proposal);
        }

        return new CatalogRecognitionIntegerAlternativesProposalSet(source.Scope, Version, proposals, diagnostics);
    }

    private static CatalogRecognitionIntegerPattern? CreateCandidate(CatalogRecognitionPreparedSample sample)
    {
        if (sample.SpanStart < 0 || sample.SpanLength <= 0 || sample.SpanStart > sample.ProductName.Length || sample.SpanLength > sample.ProductName.Length - sample.SpanStart)
        {
            return null;
        }

        var pattern = new CatalogRecognitionIntegerPattern(sample.ProductName[..sample.SpanStart], sample.ProductName[(sample.SpanStart + sample.SpanLength)..]);
        var capture = CatalogRecognitionIntegerPatternMatcher.Match(sample.ProductName, pattern);

        if (capture is null || capture.SpanStart != sample.SpanStart || capture.SpanLength != sample.SpanLength)
        {
            return null;
        }

        if (!string.Equals(capture.RawValue, sample.RawValue, StringComparison.Ordinal) || !string.Equals(capture.NormalizedValue, sample.NormalizedValue, StringComparison.Ordinal))
        {
            return null;
        }

        return pattern;
    }

    internal static CatalogRecognitionIntegerAlternativesProposal Evaluate(
    CatalogRecognitionIntegerAlternativesPattern pattern,
        IReadOnlyList<CatalogRecognitionPreparedSample> samples)
    {
        var supportingIds = new HashSet<Guid>();
        var conflictingIds = new HashSet<Guid>();
        var values = new HashSet<string>(StringComparer.Ordinal);
        var matchedNameCount = 0;

        foreach (var sample in samples)
        {
            var captures = CatalogRecognitionIntegerAlternativesMatcher.Match(sample.ProductName, pattern);

            if (captures.Count == 0)
            {
                continue;
            }

            matchedNameCount++;

            if (captures.Count != 1)
            {
                conflictingIds.UnionWith(sample.ExampleIds);
                continue;
            }

            var capture = captures[0];
            var matchesAnnotation = capture.SpanStart == sample.SpanStart && capture.SpanLength == sample.SpanLength && string.Equals(capture.RawValue, sample.RawValue, StringComparison.Ordinal) && string.Equals(capture.NormalizedValue, sample.NormalizedValue, StringComparison.Ordinal);

            if (!matchesAnnotation)
            {
                conflictingIds.UnionWith(sample.ExampleIds);
                continue;
            }

            supportingIds.UnionWith(sample.ExampleIds);
            values.Add(capture.NormalizedValue);
        }

        return new CatalogRecognitionIntegerAlternativesProposal(
            pattern,
            matchedNameCount,
            values.Count,
            supportingIds.OrderBy(id => id).ToArray(),
            conflictingIds.OrderBy(id => id).ToArray());
    }
}