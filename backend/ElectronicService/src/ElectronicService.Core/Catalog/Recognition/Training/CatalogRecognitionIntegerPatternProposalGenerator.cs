namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionIntegerPatternProposalGenerator
{
    public const string Version = "integer-context-v1";

    public static CatalogRecognitionIntegerPatternProposalSet Generate(CatalogRecognitionPreparedSampleSet source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanGenerate)
        {
            return new CatalogRecognitionIntegerPatternProposalSet(source.Scope, Version, Array.Empty<CatalogRecognitionIntegerPatternProposal>(), source.Issues);
        }

        var patterns = new HashSet<CatalogRecognitionIntegerPattern>();
        var diagnostics = new List<CatalogRecognitionTrainingIssue>();

        foreach (var sample in source.Samples)
        {
            var pattern = CreatePattern(sample);

            if (pattern is null)
            {
                diagnostics.Add(new CatalogRecognitionTrainingIssue("UnsupportedIntegerExample", $"Пример «{sample.ProductName}»: выделение «{sample.RawValue}» не подходит этой версии числового генератора. Требуется одно полное целое число, совпадающее с нормализованным значением, и непустое окружение. Это ограничение генератора, а не автоматическое признание разметки ошибочной.", sample.ExampleIds));
                continue;
            }

            patterns.Add(pattern);
        }

        var proposals = new List<CatalogRecognitionIntegerPatternProposal>();

        foreach (var pattern in patterns.OrderBy(item => item.Prefix, StringComparer.Ordinal).ThenBy(item => item.Suffix, StringComparer.Ordinal))
        {
            var proposal = Evaluate(pattern, source.Samples);

            if (proposal.DistinctValueCount >= 2)
            {
                proposals.Add(proposal);
            }
            else
            {
                var relatedIds = proposal.SupportingExampleIds.Concat(proposal.ConflictingExampleIds).Distinct().OrderBy(id => id).ToArray();
                diagnostics.Add(new CatalogRecognitionTrainingIssue("InsufficientValuesForContext", $"Для окружения «{pattern.Prefix}{{число}}{pattern.Suffix}» найдено разных поддерживающих значений: {proposal.DistinctValueCount}; требуется минимум 2. Другие окончания, включая B и D, эта версия считает разными окружениями.", relatedIds));
            }
        }

        return new CatalogRecognitionIntegerPatternProposalSet(source.Scope, Version, proposals, diagnostics);
    }

    private static CatalogRecognitionIntegerPattern? CreatePattern(CatalogRecognitionPreparedSample sample)
    {
        if (sample.SpanStart < 0 || sample.SpanLength <= 0 || sample.SpanStart > sample.ProductName.Length || sample.SpanLength > sample.ProductName.Length - sample.SpanStart)
        {
            return null;
        }

        var rawValue = sample.ProductName.Substring(sample.SpanStart, sample.SpanLength);

        if (!string.Equals(rawValue, sample.RawValue, StringComparison.Ordinal))
        {
            return null;
        }

        var normalized = CatalogRecognitionIntegerPatternMatcher.NormalizeInteger(rawValue);

        if (normalized is null || !string.Equals(normalized, sample.NormalizedValue, StringComparison.Ordinal))
        {
            return null;
        }

        var tokens = CatalogRecognitionNameTokenizer.Tokenize(sample.ProductName);
        var span = CatalogRecognitionTokenSpanResolver.Resolve(tokens, sample.SpanStart, sample.SpanLength);

        if (span is null || span.TokenCount != 1 || tokens[span.FirstTokenIndex].Kind != CatalogRecognitionNameTokenKind.Digits)
        {
            return null;
        }

        var prefix = sample.ProductName[..sample.SpanStart];
        var suffix = sample.ProductName[(sample.SpanStart + sample.SpanLength)..];

        if (prefix.Length == 0 && suffix.Length == 0)
        {
            return null;
        }

        return new CatalogRecognitionIntegerPattern(prefix, suffix);
    }

    private static CatalogRecognitionIntegerPatternProposal Evaluate(
        CatalogRecognitionIntegerPattern pattern,
        IReadOnlyList<CatalogRecognitionPreparedSample> samples)
    {
        var supportingIds = new HashSet<Guid>();
        var conflictingIds = new HashSet<Guid>();
        var values = new HashSet<string>(StringComparer.Ordinal);
        var matchedNameCount = 0;

        foreach (var sample in samples)
        {
            var capture = CatalogRecognitionIntegerPatternMatcher.Match(sample.ProductName, pattern);

            if (capture is null)
            {
                continue;
            }

            matchedNameCount++;

            var matchesAnnotation = capture.SpanStart == sample.SpanStart && capture.SpanLength == sample.SpanLength && string.Equals(capture.RawValue, sample.RawValue, StringComparison.Ordinal) && string.Equals(capture.NormalizedValue, sample.NormalizedValue, StringComparison.Ordinal);

            if (matchesAnnotation)
            {
                supportingIds.UnionWith(sample.ExampleIds);
                values.Add(capture.NormalizedValue);
            }
            else
            {
                conflictingIds.UnionWith(sample.ExampleIds);
            }
        }

        return new CatalogRecognitionIntegerPatternProposal(
            pattern,
            matchedNameCount,
            values.Count,
            supportingIds.OrderBy(id => id).ToArray(),
            conflictingIds.OrderBy(id => id).ToArray());
    }
}